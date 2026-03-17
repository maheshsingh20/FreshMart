using CartService.Application;

namespace CartService.Infrastructure;

public class HttpProductCatalogClient(IHttpClientFactory factory) : IProductCatalogClient
{
    public async Task<IEnumerable<ProductSuggestion>> GetSuggestionsAsync(
        List<Guid> productIds, CancellationToken ct)
    {
        // Simple implementation: return empty list if product service unavailable
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
}
