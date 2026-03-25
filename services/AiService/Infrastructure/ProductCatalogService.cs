using AiService.Application.Services;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AiService.Infrastructure;

public class ProductCatalogService(IHttpClientFactory httpFactory, IConfiguration config) : IProductCatalogService
{
    private static readonly JsonSerializerOptions Opts = new() { PropertyNameCaseInsensitive = true };

    public async Task<List<CatalogProduct>> GetAllAsync(CancellationToken ct = default)
    {
        try
        {
            var client = httpFactory.CreateClient();
            var url = $"{config["Services:ProductService"]}/api/v1/products?pageSize=500";
            var resp = await client.GetAsync(url, ct);
            if (!resp.IsSuccessStatusCode) return [];
            var result = await resp.Content.ReadFromJsonAsync<ProductPageResult>(Opts, ct);
            return result?.Items.Select(p => new CatalogProduct(
                p.Id, p.Name, p.Price, p.DiscountPercent, p.Category, p.Brand, p.Unit)).ToList() ?? [];
        }
        catch { return []; }
    }
}

file class ProductPageResult
{
    [JsonPropertyName("items")] public List<RawProduct> Items { get; set; } = [];
}

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
