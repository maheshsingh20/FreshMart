using CartService.Domain;
using CartService.Infrastructure;

namespace CartService.Application;

public interface ICartAppService
{
    Task<Cart> GetCartAsync(Guid customerId, CancellationToken ct = default);
    Task<Cart> AddItemAsync(Guid customerId, Guid productId, string name, decimal price, string imageUrl, int qty, CancellationToken ct = default);
    Task<Cart> RemoveItemAsync(Guid customerId, Guid productId, CancellationToken ct = default);
    Task<Cart> UpdateQuantityAsync(Guid customerId, Guid productId, int quantity, CancellationToken ct = default);
    Task<Cart> SetBudgetAsync(Guid customerId, decimal? budget, CancellationToken ct = default);
    Task ClearCartAsync(Guid customerId, CancellationToken ct = default);
    Task<IEnumerable<ProductSuggestion>> GetSuggestionsAsync(Guid customerId, CancellationToken ct = default);
}

public record ProductSuggestion(Guid ProductId, string Name, decimal Price, string ImageUrl, string Reason);

public class CartAppService(ICartRepository repo, IProductCatalogClient productClient) : ICartAppService
{
    public async Task<Cart> GetCartAsync(Guid customerId, CancellationToken ct = default)
    {
        var cart = await repo.GetAsync(customerId, ct);
        if (cart is null)
        {
            cart = new Cart { CustomerId = customerId };
            await repo.SaveAsync(cart, ct);
        }
        return cart;
    }

    public async Task<Cart> AddItemAsync(Guid customerId, Guid productId, string name,
        decimal price, string imageUrl, int qty, CancellationToken ct = default)
    {
        var cart = await GetCartAsync(customerId, ct);
        cart.AddItem(productId, name, price, imageUrl, qty);
        await repo.SaveAsync(cart, ct);
        return cart;
    }

    public async Task<Cart> RemoveItemAsync(Guid customerId, Guid productId, CancellationToken ct = default)
    {
        var cart = await GetCartAsync(customerId, ct);
        cart.RemoveItem(productId);
        await repo.SaveAsync(cart, ct);
        return cart;
    }

    public async Task<Cart> UpdateQuantityAsync(Guid customerId, Guid productId, int quantity, CancellationToken ct = default)
    {
        var cart = await GetCartAsync(customerId, ct);
        cart.UpdateQuantity(productId, quantity);
        await repo.SaveAsync(cart, ct);
        return cart;
    }

    public async Task<Cart> SetBudgetAsync(Guid customerId, decimal? budget, CancellationToken ct = default)
    {
        var cart = await GetCartAsync(customerId, ct);
        cart.SetBudget(budget);
        await repo.SaveAsync(cart, ct);
        return cart;
    }

    public async Task ClearCartAsync(Guid customerId, CancellationToken ct = default) =>
        await repo.DeleteAsync(customerId, ct);

    public async Task<IEnumerable<ProductSuggestion>> GetSuggestionsAsync(Guid customerId, CancellationToken ct = default)
    {
        var cart = await GetCartAsync(customerId, ct);
        // Smart suggestions based on cart contents - fetch complementary products
        var categoryIds = cart.Items.Select(i => i.ProductId).ToList();
        return await productClient.GetSuggestionsAsync(categoryIds, ct);
    }
}

public interface IProductCatalogClient
{
    Task<IEnumerable<ProductSuggestion>> GetSuggestionsAsync(List<Guid> productIds, CancellationToken ct);
}
