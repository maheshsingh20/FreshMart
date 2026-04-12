using CartService.Domain;
using CartService.Infrastructure;

namespace CartService.Application;

/// <summary>
/// Defines the application-level contract for all shopping cart operations.
/// Implementations are responsible for coordinating between the cart repository
/// (persistence) and the product catalogue client (stock validation).
/// All mutating methods throw <see cref="InvalidOperationException"/> when a
/// stock constraint is violated, allowing the API layer to return a 400 response
/// with a human-readable message.
/// </summary>
public interface ICartAppService
{
    /// <summary>
    /// Retrieves the cart for the given customer, creating an empty one if none exists.
    /// </summary>
    Task<Cart> GetCartAsync(Guid customerId, CancellationToken ct = default);

    /// <summary>
    /// Adds a product to the cart after verifying that sufficient stock is available.
    /// If the product is already in the cart the quantities are summed before the
    /// stock check so the total never exceeds what is physically available.
    /// </summary>
    Task<Cart> AddItemAsync(Guid customerId, Guid productId, string name, decimal price, string imageUrl, int qty,
        decimal originalPrice = 0, decimal discountPercent = 0, CancellationToken ct = default);

    /// <summary>Removes a product line from the cart. No-op if the product is not present.</summary>
    Task<Cart> RemoveItemAsync(Guid customerId, Guid productId, CancellationToken ct = default);

    /// <summary>
    /// Sets the quantity of a cart line item. A stock check is performed when
    /// increasing quantity. Passing zero or a negative value removes the item.
    /// </summary>
    Task<Cart> UpdateQuantityAsync(Guid customerId, Guid productId, int quantity, CancellationToken ct = default);

    /// <summary>Sets or clears the optional spending budget for the cart.</summary>
    Task<Cart> SetBudgetAsync(Guid customerId, decimal? budget, CancellationToken ct = default);

    /// <summary>Deletes the cart record entirely. Called after a successful order placement.</summary>
    Task ClearCartAsync(Guid customerId, CancellationToken ct = default);

    /// <summary>
    /// Returns product suggestions based on the items currently in the cart.
    /// Delegates to the product catalogue client which may use category affinity,
    /// collaborative filtering, or AI-based recommendations.
    /// </summary>
    Task<IEnumerable<ProductSuggestion>> GetSuggestionsAsync(Guid customerId, CancellationToken ct = default);
}

/// <summary>
/// A lightweight DTO representing a product recommendation surfaced to the customer
/// while they are browsing their cart. The <c>Reason</c> field provides a
/// human-readable explanation (e.g. "Frequently bought together") that the UI
/// can display alongside the suggestion.
/// </summary>
public record ProductSuggestion(Guid ProductId, string Name, decimal Price, string ImageUrl, string Reason);

/// <summary>
/// Concrete implementation of <see cref="ICartAppService"/>.
/// Orchestrates cart persistence via <see cref="ICartRepository"/> and enforces
/// stock constraints by calling <see cref="IProductCatalogClient"/> before every
/// mutation. This keeps business rules (stock validation) in the application layer
/// rather than leaking them into the domain or the HTTP controller.
/// </summary>
public class CartAppService(ICartRepository repo, IProductCatalogClient productClient) : ICartAppService
{
    /// <summary>
    /// Loads the customer's cart from the repository. If no cart exists yet
    /// (first visit, or after a clear), a new empty cart is created and persisted
    /// so subsequent reads are consistent.
    /// </summary>
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

    /// <summary>
    /// Validates stock availability, then delegates to the domain model to add
    /// or increment the cart line. The combined quantity (existing + requested)
    /// is checked against live stock so a customer cannot circumvent the limit
    /// by adding the same product multiple times.
    /// </summary>
    public async Task<Cart> AddItemAsync(Guid customerId, Guid productId, string name,
        decimal price, string imageUrl, int qty,
        decimal originalPrice = 0, decimal discountPercent = 0, CancellationToken ct = default)
    {
        // Check stock before adding
        var stock = await productClient.GetStockAsync(productId, ct);
        var cart = await GetCartAsync(customerId, ct);
        var existingQty = cart.Items.FirstOrDefault(i => i.ProductId == productId)?.Quantity ?? 0;
        if (existingQty + qty > stock)
            throw new InvalidOperationException($"Only {stock} unit(s) available in stock.");

        cart.AddItem(productId, name, price, imageUrl, qty, originalPrice, discountPercent);
        await repo.SaveAsync(cart, ct);
        return cart;
    }

    /// <inheritdoc/>
    public async Task<Cart> RemoveItemAsync(Guid customerId, Guid productId, CancellationToken ct = default)
    {
        var cart = await GetCartAsync(customerId, ct);
        cart.RemoveItem(productId);
        await repo.SaveAsync(cart, ct);
        return cart;
    }

    /// <summary>
    /// Updates the quantity of a cart line. When increasing, a fresh stock check
    /// is performed against the catalogue. When decreasing (or zeroing), no stock
    /// check is needed because we are releasing units back to the pool.
    /// </summary>
    public async Task<Cart> UpdateQuantityAsync(Guid customerId, Guid productId, int quantity, CancellationToken ct = default)
    {
        if (quantity > 0)
        {
            // Check stock before increasing quantity
            var stock = await productClient.GetStockAsync(productId, ct);
            if (quantity > stock)
                throw new InvalidOperationException($"Only {stock} unit(s) available in stock.");
        }

        var cart = await GetCartAsync(customerId, ct);
        cart.UpdateQuantity(productId, quantity);
        await repo.SaveAsync(cart, ct);
        return cart;
    }

    /// <inheritdoc/>
    public async Task<Cart> SetBudgetAsync(Guid customerId, decimal? budget, CancellationToken ct = default)
    {
        var cart = await GetCartAsync(customerId, ct);
        cart.SetBudget(budget);
        await repo.SaveAsync(cart, ct);
        return cart;
    }

    /// <inheritdoc/>
    public async Task ClearCartAsync(Guid customerId, CancellationToken ct = default) =>
        await repo.DeleteAsync(customerId, ct);

    /// <summary>
    /// Collects the product IDs currently in the cart and forwards them to the
    /// product catalogue client, which returns complementary or related products.
    /// The result is surfaced in the cart UI as "You might also like" suggestions.
    /// </summary>
    public async Task<IEnumerable<ProductSuggestion>> GetSuggestionsAsync(Guid customerId, CancellationToken ct = default)
    {
        var cart = await GetCartAsync(customerId, ct);
        // Smart suggestions based on cart contents - fetch complementary products
        var categoryIds = cart.Items.Select(i => i.ProductId).ToList();
        return await productClient.GetSuggestionsAsync(categoryIds, ct);
    }
}

/// <summary>
/// Abstraction over the ProductService HTTP API used by the cart application layer.
/// Decoupling behind an interface allows the cart to be tested in isolation and
/// makes it straightforward to swap the transport (HTTP, gRPC, message bus) without
/// touching business logic.
/// </summary>
public interface IProductCatalogClient
{
    /// <summary>
    /// Returns product suggestions that complement the given list of product IDs.
    /// The catalogue service decides the recommendation strategy (category affinity,
    /// popularity, AI ranking, etc.).
    /// </summary>
    Task<IEnumerable<ProductSuggestion>> GetSuggestionsAsync(List<Guid> productIds, CancellationToken ct);

    /// <summary>
    /// Returns the current available stock quantity for a single product.
    /// Used to gate add-to-cart and quantity-update operations.
    /// </summary>
    Task<int> GetStockAsync(Guid productId, CancellationToken ct);
}
