using CartService.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CartService.API.Controllers;

[ApiController]
[Route("api/v1/cart")]
[Authorize]
public class CartController(ICartAppService cartService) : ControllerBase
{
    private Guid UserId => Guid.Parse(
        User.FindFirstValue(JwtRegisteredClaimNames.Sub)
        ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetCart(CancellationToken ct)
    {
        var cart = await cartService.GetCartAsync(UserId, ct);
        return Ok(MapCart(cart));
    }

    [HttpPost("items")]
    public async Task<IActionResult> AddItem(AddItemRequest req, CancellationToken ct)
    {
        var cart = await cartService.AddItemAsync(UserId, req.ProductId, req.ProductName,
            req.UnitPrice, req.ImageUrl ?? "", req.Quantity, ct);
        return Ok(MapCart(cart));
    }

    [HttpDelete("items/{productId:guid}")]
    public async Task<IActionResult> RemoveItem(Guid productId, CancellationToken ct)
    {
        var cart = await cartService.RemoveItemAsync(UserId, productId, ct);
        return Ok(MapCart(cart));
    }

    [HttpPatch("items/{productId:guid}")]
    public async Task<IActionResult> UpdateQuantity(Guid productId, UpdateQuantityRequest req, CancellationToken ct)
    {
        var cart = await cartService.UpdateQuantityAsync(UserId, productId, req.Quantity, ct);
        return Ok(MapCart(cart));
    }

    [HttpDelete]
    public async Task<IActionResult> ClearCart(CancellationToken ct)
    {
        await cartService.ClearCartAsync(UserId, ct);
        return NoContent();
    }

    [HttpPut("budget")]
    public async Task<IActionResult> SetBudget(SetBudgetRequest req, CancellationToken ct)
    {
        var cart = await cartService.SetBudgetAsync(UserId, req.Budget, ct);
        return Ok(MapCart(cart));
    }

    [HttpGet("suggestions")]
    public async Task<IActionResult> GetSuggestions(CancellationToken ct)
    {
        var suggestions = await cartService.GetSuggestionsAsync(UserId, ct);
        return Ok(suggestions);
    }

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
            discountPercent = 0,
            originalPrice = i.UnitPrice
        }),
        budgetLimit = cart.BudgetLimit,
        lastUpdated = cart.LastUpdated,
        subTotal = cart.SubTotal,
        isOverBudget = cart.IsOverBudget,
        totalItems = cart.TotalItems
    };
}

public record AddItemRequest(Guid ProductId, string ProductName, decimal UnitPrice, string? ImageUrl, int Quantity = 1);
public record UpdateQuantityRequest(int Quantity);
public record SetBudgetRequest(decimal? Budget);
