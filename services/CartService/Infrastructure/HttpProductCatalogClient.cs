using CartService.Application;

namespace CartService.Infrastructure;

/// <summary>
/// HTTP client that calls ProductService to fetch stock levels and product suggestions.
/// Used by <see cref="CartAppService"/> to validate stock before adding items to the cart.
/// Fails open — returns <see cref="int.MaxValue"/> for stock when ProductService is unreachable
/// so a temporary outage doesn't block customers from using their cart.
/// </summary>
public class HttpProductCatalogClient(IHttpClientFactory factory) : IProductCatalogClient
{
    /// <inheritdoc/>
    public async Task<IEnumerable<ProductSuggestion>> GetSuggestionsAsync(
        List<Guid> productIds, CancellationToken ct)
    {
        try
        {
            var client = factory.CreateClient("product-service");
            var ids = string.Join(",", productIds);
            var response = await client.GetFromJsonAsync<IEnumerable<ProductSuggestion>>(
                $"/api/v1/products/suggestions?ids={ids}", ct);
            return response ?? [];
        }
        catch { return []; }
    }

    /// <summary>
    /// Returns the current stock quantity for a product.
    /// Returns <see cref="int.MaxValue"/> on failure so cart operations are not blocked
    /// when ProductService is temporarily unavailable.
    /// </summary>
    public async Task<int> GetStockAsync(Guid productId, CancellationToken ct)
    {
        try
        {
            var client = factory.CreateClient("product-service");
            var product = await client.GetFromJsonAsync<ProductStockDto>(
                $"/api/v1/products/{productId}", ct);
            return product?.StockQuantity ?? 0;
        }
        catch { return int.MaxValue; }
    }
}

/// <summary>Minimal DTO to deserialize the stock quantity from a ProductService response.</summary>
file record ProductStockDto(int StockQuantity);
