using AiService.Application.Services;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AiService.Infrastructure;

/// <summary>
/// Concrete implementation of <see cref="IProductCatalogService"/>.
/// Fetches the product catalog from ProductService via HTTP on every AI request.
///
/// WHY FETCH ON EVERY REQUEST (not cache):
///   The AI always needs the latest catalog — current prices, discounts, stock status.
///   If a product goes out of stock or gets a new discount, the AI should know immediately.
///   ProductService already caches results in Redis, so this HTTP call is fast.
///
/// FAIL-OPEN DESIGN:
///   If ProductService is down, returns an empty list instead of throwing.
///   The AI can still respond to general questions, just without product recommendations.
///   This prevents AiService from being unavailable just because ProductService is slow.
///
/// INTERNAL DTOs (file-scoped):
///   ProductPageResult and RawProduct are marked with 'file' keyword —
///   they are only visible within this file. This keeps the mapping logic
///   self-contained and prevents these internal types from leaking out.
/// </summary>
public class ProductCatalogService(IHttpClientFactory httpFactory, IConfiguration config) : IProductCatalogService
{
    private static readonly JsonSerializerOptions Opts = new() { PropertyNameCaseInsensitive = true };

    /// <inheritdoc/>
    public async Task<List<CatalogProduct>> GetAllAsync(CancellationToken ct = default)
    {
        try
        {
            var client = httpFactory.CreateClient();
            // pageSize=500 ensures we get all products in one call
            // ProductService paginates by default — without this we'd only get 20
            var url = $"{config["Services:ProductService"]}/api/v1/products?pageSize=500";
            var resp = await client.GetAsync(url, ct);
            if (!resp.IsSuccessStatusCode) return [];
            var result = await resp.Content.ReadFromJsonAsync<ProductPageResult>(Opts, ct);
            // Map RawProduct → CatalogProduct (only keep fields the AI needs)
            return result?.Items.Select(p => new CatalogProduct(
                p.Id, p.Name, p.Price, p.DiscountPercent, p.Category, p.Brand, p.Unit)).ToList() ?? [];
        }
        catch
        {
            // Fail-open: return empty list so AI still works even if ProductService is down
            return [];
        }
    }
}

/// <summary>
/// Maps the paginated response from GET /api/v1/products.
/// The response shape is: { items: [...], total: 52, page: 1, pageSize: 500 }
/// We only need the items array.
/// </summary>
file class ProductPageResult
{
    [JsonPropertyName("items")] public List<RawProduct> Items { get; set; } = [];
}

/// <summary>
/// Raw product DTO from ProductService API response.
/// Contains only the fields we map to CatalogProduct.
/// Marked as file-scoped — not visible outside this file.
/// </summary>
file class RawProduct
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public decimal DiscountPercent { get; set; }
    public string? Category { get; set; }
    public string? Brand { get; set; }
    public string? Unit { get; set; }
}
