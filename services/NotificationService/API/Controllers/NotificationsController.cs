using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotificationService.Infrastructure;

namespace NotificationService.API.Controllers;

[ApiController]
[Route("api/v1/notifications")]
[Authorize]
public class NotificationsController(NotificationDbContext db) : ControllerBase
{
    private Guid UserId => Guid.Parse(
        User.FindFirstValue(JwtRegisteredClaimNames.Sub)
        ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var items = await db.Notifications
            .Where(n => n.UserId == UserId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .Select(n => new { id = n.Id.ToString(), n.Title, n.Message, n.Type, n.Link, n.IsRead, createdAt = n.CreatedAt.ToString("o") })
            .ToListAsync();
        return Ok(items);
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> UnreadCount()
    {
        var count = await db.Notifications.CountAsync(n => n.UserId == UserId && !n.IsRead);
        return Ok(new { count });
    }

    [HttpPatch("{id}/read")]
    public async Task<IActionResult> MarkRead(Guid id)
    {
        var n = await db.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == UserId);
        if (n == null) return NotFound();
        n.IsRead = true;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        await db.Notifications
            .Where(n => n.UserId == UserId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var n = await db.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == UserId);
        if (n == null) return NotFound();
        db.Notifications.Remove(n);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteAll()
    {
        await db.Notifications.Where(n => n.UserId == UserId).ExecuteDeleteAsync();
        return NoContent();
    }
}

// Internal controller — called directly by other microservices (no user auth required)
[ApiController]
[Route("api/v1/notifications")]
public class NotificationsInternalController(
    NotificationDbContext db,
    EmailService emailService,
    ILogger<NotificationsInternalController> logger) : ControllerBase
{
    [HttpPost("welcome")]
    [AllowAnonymous]
    public async Task<IActionResult> Welcome([FromBody] WelcomeNotification req, CancellationToken ct)
    {
        logger.LogInformation("Welcome notification for {Email}", req.Email);
        await emailService.SendWelcomeAsync(req.Email, req.FirstName);
        return Ok();
    }

    [HttpPost("order-created")]
    [AllowAnonymous]
    public async Task<IActionResult> OrderCreated([FromBody] OrderCreatedNotification req, CancellationToken ct)    {
        logger.LogInformation("OrderCreated notification for {Email}", req.CustomerEmail);

        // Send email
        if (!string.IsNullOrEmpty(req.CustomerEmail))
        {
            await emailService.SendOrderConfirmationAsync(
                req.CustomerEmail,
                req.CustomerFirstName,
                req.OrderRef,
                req.Total,
                req.Items.Select(i => (i.ProductName, i.Quantity, i.UnitPrice)));
        }

        // Persist in-app notification
        db.Notifications.Add(new NotificationService.Domain.Notification
        {
            UserId = req.CustomerId,
            Title = "Order Confirmed ✅",
            Message = $"Order #{req.OrderRef} placed. Total: ₹{req.Total:F2}",
            Type = "success",
            Link = "/orders"
        });
        await db.SaveChangesAsync(ct);
        return Ok();
    }

    [HttpPost("order-status-changed")]
    [AllowAnonymous]
    public async Task<IActionResult> OrderStatusChanged([FromBody] OrderStatusNotification req, CancellationToken ct)
    {
        logger.LogInformation("OrderStatusChanged notification for order {OrderRef} → {Status}", req.OrderRef, req.NewStatus);

        if (!string.IsNullOrEmpty(req.CustomerEmail))
            await emailService.SendOrderStatusAsync(req.CustomerEmail, req.CustomerFirstName, req.OrderRef, req.NewStatus);

        var (title, msg, type) = req.NewStatus switch
        {
            "Processing"     => ("Order Processing",  $"Your order #{req.OrderRef} is being prepared.", "info"),
            "Shipped"        => ("Order Shipped",     $"Your order #{req.OrderRef} has been shipped!", "info"),
            "OutForDelivery" => ("Out for Delivery",  $"Your order #{req.OrderRef} is out for delivery!", "warning"),
            "Delivered"      => ("Order Delivered",   $"Your order #{req.OrderRef} has been delivered. Enjoy!", "success"),
            "Cancelled"      => ("Order Cancelled",   $"Your order #{req.OrderRef} has been cancelled.", "error"),
            _                => ("Order Updated",     $"Your order #{req.OrderRef} status: {req.NewStatus}", "info")
        };

        db.Notifications.Add(new NotificationService.Domain.Notification
        {
            UserId = req.CustomerId,
            Title = title,
            Message = msg,
            Type = type,
            Link = "/orders"
        });
        await db.SaveChangesAsync(ct);
        return Ok();
    }
}

public record WelcomeNotification(Guid UserId, string Email, string FirstName);
public record OrderCreatedNotification(
    Guid CustomerId,
    string CustomerEmail,
    string CustomerFirstName,
    Guid OrderId,
    string OrderRef,
    decimal Total,
    List<OrderItemLine> Items);

public record OrderStatusNotification(
    Guid CustomerId,
    string CustomerEmail,
    string CustomerFirstName,
    Guid OrderId,
    string OrderRef,
    string NewStatus);

public record OrderItemLine(string ProductName, int Quantity, decimal UnitPrice);
