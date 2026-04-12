using CartService.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CartService.API.Controllers;

/// <summary>
/// HTTP API controller for shopping cart operations.
/// Every endpoint is scoped to the authenticated user — the customer ID is extracted
/// directly from the JWT so callers can never manipulate another user's cart.
/// All mutating operations (add, update quantity) perform a real-time stock check
/// against the ProductService before persisting, ensuring the cart never holds
/// more items than are physically available.
/// </summary>
[ApiController]
[Route("api/v1/cart")]
[Authorize]
public class CartController(ICartAppService cartService) : ControllerBase
{
    /// <summary>
    /// Resolves the authenticated user's ID from the JWT claims.
    /// Tries the standard <c>sub</c> claim first (OpenID Connect), then falls back
    /// to the ASP.NET <see cref="ClaimTypes.NameIdentifier"/> claim so the controller
    /// works with both JWT profiles.
    /// </summary>
    private Guid UserId => Guid.Parse(
        User.FindFirstValue(JwtRegisteredClaimNames.Sub)
        ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>
    /// Retrieves the current user's cart, including all line items, subtotal,
    /// budget status, and discount information.
    /// If the user has no cart yet, an empty one is created and returned transparently.
    /// </summary>
    /// <param name="ct">Cancellation token propagated from the HTTP request pipeline.</param>
    /// <returns>A 200 OK response containing the serialised cart object.</returns>
    [HttpGet]
    public async Task<IActionResult> GetCart(CancellationToken ct)
    {
        var cart = await cartService.GetCartAsync(UserId, ct);
        return Ok(MapCart(cart));
    }

    /// <summary>
    /// Adds a product to the cart or increments its quantity if it already exists.
    /// Before persisting, the service verifies that the requested quantity does not
    /// exceed the available stock in ProductService. Returns 400 if stock is insufficient.
    /// </summary>
    /// <param name="req">
    /// The product details and quantity to add. <c>OriginalPrice</c> and
    /// <c>DiscountPercent</c> are optional — when omitted the item is treated as
    /// full-price.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>200 OK with the updated cart, or 400 Bad Request with an error message.</returns>
    [HttpPost("items")]
    public async Task<IActionResult> AddItem(AddItemRequest req, CancellationToken ct)
    {
        try
        {
            var cart = await cartService.AddItemAsync(UserId, req.ProductId, req.ProductName,
                req.UnitPrice, req.ImageUrl ?? "", req.Quantity,
                req.OriginalPrice ?? req.UnitPrice, req.DiscountPercent ?? 0, ct);
            return Ok(MapCart(cart));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Removes a specific product line from the cart entirely.
    /// If the product is not in the cart the operation is a no-op and the
    /// unchanged cart is returned.
    /// </summary>
    /// <param name="productId">The unique identifier of the product to remove.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>200 OK with the updated cart.</returns>
    [HttpDelete("items/{productId:guid}")]
    public async Task<IActionResult> RemoveItem(Guid productId, CancellationToken ct)
    {
        var cart = await cartService.RemoveItemAsync(UserId, productId, ct);
        return Ok(MapCart(cart));
    }

    /// <summary>
    /// Updates the quantity of an existing cart line item.
    /// Passing a quantity of zero or less removes the item from the cart.
    /// A stock check is performed when increasing quantity to prevent over-ordering.
    /// </summary>
    /// <param name="productId">The product whose quantity should be changed.</param>
    /// <param name="req">The new desired quantity.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>200 OK with the updated cart, or 400 Bad Request if stock is insufficient.</returns>
    [HttpPatch("items/{productId:guid}")]
    public async Task<IActionResult> UpdateQuantity(Guid productId, UpdateQuantityRequest req, CancellationToken ct)
    {
        try
        {
            var cart = await cartService.UpdateQuantityAsync(UserId, productId, req.Quantity, ct);
            return Ok(MapCart(cart));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Removes all items from the cart and deletes the cart record entirely.
    /// Typically called after a successful order placement to reset the shopping session.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>204 No Content on success.</returns>
    [HttpDelete]
    public async Task<IActionResult> ClearCart(CancellationToken ct)
    {
        await cartService.ClearCartAsync(UserId, ct);
        return NoContent();
    }

    /// <summary>
    /// Sets or clears a spending budget limit for the cart.
    /// Once set, the cart's <c>IsOverBudget</c> flag is computed automatically
    /// whenever the subtotal changes, giving the frontend a signal to warn the user.
    /// Pass <c>null</c> to remove the budget constraint.
    /// </summary>
    /// <param name="req">The budget amount, or <c>null</c> to clear it.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>200 OK with the updated cart including the new budget state.</returns>
    [HttpPut("budget")]
    public async Task<IActionResult> SetBudget(SetBudgetRequest req, CancellationToken ct)
    {
        var cart = await cartService.SetBudgetAsync(UserId, req.Budget, ct);
        return Ok(MapCart(cart));
    }

    /// <summary>
    /// Returns AI-driven or catalogue-based product suggestions relevant to the
    /// current cart contents. The suggestion engine uses the product IDs already
    /// in the cart to find complementary or frequently-bought-together items.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>200 OK with a list of <see cref="ProductSuggestion"/> objects.</returns>
    [HttpGet("suggestions")]
    public async Task<IActionResult> GetSuggestions(CancellationToken ct)
    {
        var suggestions = await cartService.GetSuggestionsAsync(UserId, ct);
        return Ok(suggestions);
    }

    /// <summary>
    /// Maps a domain <see cref="CartService.Domain.Cart"/> entity to an anonymous
    /// response DTO. Keeping the mapping here (rather than in the domain) ensures
    /// the API shape can evolve independently of the domain model.
    /// </summary>
    /// <param name="cart">The domain cart to serialise.</param>
    /// <returns>An anonymous object safe for JSON serialisation.</returns>
    private static object MapCart(CartService.Domain.Cart cart) => new
    {
        customerId = cart.CustomerId,
        items = cart.Items.Select(i => new
        {
            productId = i.ProductId,
            productName = i.ProductName,
            unitPrice = i.UnitPrice,
            imageUrl = i.ImageUrl,
            quantity = i.Quantity,
            totalPrice = i.TotalPrice,
            originalPrice = i.OriginalPrice,
            originalTotalPrice = i.OriginalTotalPrice,
            discountPercent = i.DiscountPercent
        }),
        budgetLimit = cart.BudgetLimit,
        lastUpdated = cart.LastUpdated,
        subTotal = cart.SubTotal,
        isOverBudget = cart.IsOverBudget,
        totalItems = cart.TotalItems
    };
}

/// <summary>
/// Request body for adding a product to the cart.
/// <c>OriginalPrice</c> and <c>DiscountPercent</c> are optional and used to
/// display savings information in the UI without re-fetching the product catalogue.
/// </summary>
public record AddItemRequest(Guid ProductId, string ProductName, decimal UnitPrice, string? ImageUrl,
    int Quantity = 1, decimal? OriginalPrice = null, decimal? DiscountPercent = null);

/// <summary>
/// Request body for changing the quantity of an existing cart line item.
/// </summary>
public record UpdateQuantityRequest(int Quantity);

/// <summary>
/// Request body for setting or clearing the cart's spending budget.
/// A <c>null</c> value removes the budget constraint.
/// </summary>
public record SetBudgetRequest(decimal? Budget);
