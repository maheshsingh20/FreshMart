using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/v1/cart")]
[Authorize]
public class CartController(AppDbContext db) : ControllerBase
{
    // With MapInboundClaims=false, 'sub' stays as JwtRegisteredClaimNames.Sub ("sub")
    private Guid? TryGetUserId()
    {
        var val = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");
        return Guid.TryParse(val, out var id) ? id : null;
    }

    private IActionResult? UserIdError()
    {
        var id = TryGetUserId();
        if (id == null)
        {
            var claims = User.Claims.Select(c => $"{c.Type}={c.Value}").ToList();
            return Problem($"User ID claim not found. Claims: [{string.Join(", ", claims)}]", statusCode: 400);
        }
        return null;
    }

    private Guid UserId => TryGetUserId()!.Value;

    private async Task<CartDto> BuildCartDto(Cart cart)
    {
        var items = cart.Items.Select(i => {
            var unitPrice = i.Product.DiscountPercent > 0
                ? Math.Round(i.Product.Price * (1 - i.Product.DiscountPercent / 100m), 2)
                : i.Product.Price;
            return new CartItemDto(
                i.ProductId.ToString(), i.Product.Name, unitPrice,
                i.Product.ImageUrl, i.Quantity, unitPrice * i.Quantity,
                i.Product.DiscountPercent, i.Product.Price);
        }).ToList();
        var subTotal = items.Sum(i => i.TotalPrice);
        return new CartDto(cart.CustomerId.ToString(), items, cart.BudgetLimit,
            cart.LastUpdated.ToString("o"), subTotal,
            cart.BudgetLimit.HasValue && subTotal > cart.BudgetLimit.Value,
            items.Sum(i => i.Quantity));
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var cart = await db.Carts.Include(c => c.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.CustomerId == UserId);
        if (cart == null) return Ok(new CartDto(UserId.ToString(), [], null, DateTime.UtcNow.ToString("o"), 0, false, 0));
        return Ok(await BuildCartDto(cart));
    }

    [HttpPost("items")]
    public async Task<IActionResult> AddItem(AddToCartRequest req)
    {
        var err = UserIdError(); if (err != null) return err;
        var product = await db.Products.FindAsync(req.ProductId);
        if (product == null) return NotFound(new { error = "Product not found" });
        if (product.StockQuantity < req.Quantity) return BadRequest(new { error = "Insufficient stock" });

        // Ensure cart exists first (save before adding items to get a valid CartId)
        var cart = await db.Carts.FirstOrDefaultAsync(c => c.CustomerId == UserId);
        if (cart == null)
        {
            cart = new Cart { CustomerId = UserId };
            db.Carts.Add(cart);
            await db.SaveChangesAsync(); // persist so cart.Id is valid
        }

        // Now handle item — query directly to avoid stale tracking
        var existing = await db.CartItems.FirstOrDefaultAsync(i => i.CartId == cart.Id && i.ProductId == req.ProductId);
        if (existing != null)
        {
            existing.Quantity += req.Quantity;
        }
        else
        {
            db.CartItems.Add(new CartItem { CartId = cart.Id, ProductId = req.ProductId, Quantity = req.Quantity });
        }

        cart.LastUpdated = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var full = await db.Carts.Include(c => c.Items).ThenInclude(i => i.Product).FirstAsync(c => c.Id == cart.Id);
        return Ok(await BuildCartDto(full));
    }

    [HttpPut("items/{productId}")]
    public async Task<IActionResult> UpdateItem(Guid productId, UpdateCartItemRequest req)
    {
        var cart = await db.Carts.Include(c => c.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.CustomerId == UserId);
        if (cart == null) return NotFound();
        var item = await db.CartItems.FirstOrDefaultAsync(i => i.CartId == cart.Id && i.ProductId == productId);
        if (item == null) return NotFound();
        if (req.Quantity <= 0) db.CartItems.Remove(item);
        else item.Quantity = req.Quantity;
        cart.LastUpdated = DateTime.UtcNow;
        await db.SaveChangesAsync();
        var full = await db.Carts.Include(c => c.Items).ThenInclude(i => i.Product).FirstAsync(c => c.Id == cart.Id);
        return Ok(await BuildCartDto(full));
    }

    [HttpDelete("items/{productId}")]
    public async Task<IActionResult> RemoveItem(Guid productId)
    {
        var cart = await db.Carts.FirstOrDefaultAsync(c => c.CustomerId == UserId);
        if (cart == null) return NotFound();
        var item = await db.CartItems.FirstOrDefaultAsync(i => i.CartId == cart.Id && i.ProductId == productId);
        if (item != null) { db.CartItems.Remove(item); cart.LastUpdated = DateTime.UtcNow; await db.SaveChangesAsync(); }
        var full = await db.Carts.Include(c => c.Items).ThenInclude(i => i.Product).FirstAsync(c => c.Id == cart.Id);
        return Ok(await BuildCartDto(full));
    }

    [HttpDelete]
    public async Task<IActionResult> Clear()
    {
        var cart = await db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.CustomerId == UserId);
        if (cart != null) { db.CartItems.RemoveRange(cart.Items); cart.LastUpdated = DateTime.UtcNow; await db.SaveChangesAsync(); }
        return NoContent();
    }

    [HttpPut("budget")]
    public async Task<IActionResult> SetBudget(SetBudgetRequest req)
    {
        var cart = await db.Carts.FirstOrDefaultAsync(c => c.CustomerId == UserId);
        if (cart == null) { cart = new Cart { CustomerId = UserId }; db.Carts.Add(cart); }
        cart.BudgetLimit = req.BudgetLimit;
        await db.SaveChangesAsync();
        return NoContent();
    }
}
