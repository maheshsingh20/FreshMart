using CartService.Application;

namespace CartService.Infrastructure;

public class HttpProductCatalogClient(IHttpClientFactory factory) : IProductCatalogClient
{
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

    public async Task<int> GetStockAsync(Guid productId, CancellationToken ct)
    {
        try
        {
            var client = factory.CreateClient("product-service");
            var product = await client.GetFromJsonAsync<ProductStockDto>(
                $"/api/v1/products/{productId}", ct);
            return product?.StockQuantity ?? 0;
        }
        catch { return int.MaxValue; } // fail open — don't block cart if product service is down
    }
}

// Minimal DTO to read stock from ProductService response
file record ProductStockDto(int StockQuantity);
