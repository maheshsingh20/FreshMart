using CartService.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CartService.API.Controllers;

[ApiController]
[Route("api/v1/cart")]
[Authorize]
public class CartController(ICartAppService cartService) : ControllerBase
{
    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetCart(CancellationToken ct)
    {
        var cart = await cartService.GetCartAsync(CurrentUserId, ct);
        return Ok(cart);
    }

    [HttpPost("items")]
    public async Task<IActionResult> AddItem([FromBody] AddItemRequest req, CancellationToken ct)
    {
        var cart = await cartService.AddItemAsync(
            CurrentUserId, req.ProductId, req.ProductName, req.UnitPrice, req.ImageUrl, req.Quantity, ct);
        return Ok(cart);
    }

    [HttpDelete("items/{productId:guid}")]
    public async Task<IActionResult> RemoveItem(Guid productId, CancellationToken ct)
    {
        var cart = await cartService.RemoveItemAsync(CurrentUserId, productId, ct);
        return Ok(cart);
    }

    [HttpPut("items/{productId:guid}")]
    public async Task<IActionResult> UpdateQuantity(Guid productId, [FromBody] UpdateQuantityRequest req, CancellationToken ct)
    {
        var cart = await cartService.UpdateQuantityAsync(CurrentUserId, productId, req.Quantity, ct);
        return Ok(cart);
    }

    [HttpPut("budget")]
    public async Task<IActionResult> SetBudget([FromBody] SetBudgetRequest req, CancellationToken ct)
    {
        var cart = await cartService.SetBudgetAsync(CurrentUserId, req.Budget, ct);
        return Ok(cart);
    }

    [HttpDelete]
    public async Task<IActionResult> ClearCart(CancellationToken ct)
    {
        await cartService.ClearCartAsync(CurrentUserId, ct);
        return NoContent();
    }

    [HttpGet("suggestions")]
    public async Task<IActionResult> GetSuggestions(CancellationToken ct)
    {
        var suggestions = await cartService.GetSuggestionsAsync(CurrentUserId, ct);
        return Ok(suggestions);
    }
}

public record AddItemRequest(Guid ProductId, string ProductName, decimal UnitPrice, string ImageUrl, int Quantity = 1);
public record UpdateQuantityRequest(int Quantity);
public record SetBudgetRequest(decimal? Budget);
