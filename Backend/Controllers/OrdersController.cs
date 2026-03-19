using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Backend.Data;
using Backend.DTOs;
using Backend.Messaging;
using Backend.Models;
using Backend.Services;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Razorpay.Api;
using RazorpayOrder = Razorpay.Api.Order;
using AppOrder = Backend.Models.Order;

namespace Backend.Controllers;

[ApiController]
[Route("api/v1/orders")]
[Authorize]
public class OrdersController(AppDbContext db, IPublishEndpoint bus, IConfiguration config) : ControllerBase
{
    private Guid UserId => Guid.Parse(
        User.FindFirstValue(JwtRegisteredClaimNames.Sub)
        ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("User ID claim not found"));

    private static OrderDto ToDto(AppOrder o) => new(
        o.Id.ToString(), o.CustomerId.ToString(), o.Status,
        o.SubTotal, o.DeliveryFee, o.TaxAmount, o.DiscountAmount, o.TotalAmount,
        o.DeliveryAddress, o.Notes, o.CreatedAt.ToString("o"),
        o.EstimatedDelivery?.ToString("o"), o.DeliveredAt?.ToString("o"),
        o.Items.Select(i => new OrderItemDto(i.ProductId.ToString(), i.ProductName, i.Quantity, i.UnitPrice, i.Quantity * i.UnitPrice)));

    [HttpGet]
    public async Task<IActionResult> GetOrders()
    {
        var isAdmin = User.IsInRole("Admin") || User.IsInRole("StoreManager");
        var q = db.Orders.Include(o => o.Items).AsQueryable();
        if (!isAdmin) q = q.Where(o => o.CustomerId == UserId);
        var orders = await q.OrderByDescending(o => o.CreatedAt).Select(o => ToDto(o)).ToListAsync();
        return Ok(orders);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrder(Guid id)
    {
        var order = await db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);
        if (order == null) return NotFound();
        if (order.CustomerId != UserId && !User.IsInRole("Admin") && !User.IsInRole("StoreManager"))
            return Forbid();
        return Ok(ToDto(order));
    }

    // ── Shared: calculate totals from cart ───────────────────────────────────
    private async Task<(decimal subTotal, decimal delivery, decimal tax, decimal discount, Coupon? coupon)>
        CalcTotalsAsync(Guid userId, string? couponCode)
    {
        var cart = await db.Carts.Include(c => c.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.CustomerId == userId);
        if (cart == null || !cart.Items.Any()) throw new InvalidOperationException("Cart is empty");

        var subTotal = cart.Items.Sum(i =>
        {
            var price = i.Product.DiscountPercent > 0
                ? Math.Round(i.Product.Price * (1 - i.Product.DiscountPercent / 100m), 2)
                : i.Product.Price;
            return price * i.Quantity;
        });
        var delivery = subTotal >= 500 ? 0m : 49m;
        var tax = Math.Round(subTotal * 0.05m, 2);
        decimal discount = 0;
        Coupon? coupon = null;
        if (!string.IsNullOrWhiteSpace(couponCode))
        {
            coupon = await db.Coupons.FirstOrDefaultAsync(c =>
                c.Code == couponCode.ToUpper() && c.IsActive &&
                (c.ExpiresAt == null || c.ExpiresAt > DateTime.UtcNow) &&
                c.UsedCount < c.UsageLimit && subTotal >= c.MinOrderAmount);
            if (coupon != null)
                discount = coupon.DiscountType == "Percentage"
                    ? Math.Round(subTotal * coupon.DiscountValue / 100, 2)
                    : Math.Min(coupon.DiscountValue, subTotal);
        }
        return (subTotal, delivery, tax, discount, coupon);
    }

    // ── POST /api/v1/orders/create-payment ───────────────────────────────────
    [HttpPost("create-payment")]
    public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentRequest req)
    {
        try
        {
            var (subTotal, delivery, tax, discount, _) = await CalcTotalsAsync(UserId, req.CouponCode);
            var total = Math.Max(0, subTotal + delivery + tax - discount);
            var amountPaise = (long)(total * 100); // Razorpay uses paise

            var keyId = config["Razorpay:KeyId"]!;
            var keySecret = config["Razorpay:KeySecret"]!;
            var client = new RazorpayClient(keyId, keySecret);

            var options = new Dictionary<string, object>
            {
                { "amount", amountPaise },
                { "currency", "INR" },
                { "receipt", $"order_{UserId.ToString()[..8]}_{DateTime.UtcNow.Ticks}" },
                { "notes", new Dictionary<string, string> { { "userId", UserId.ToString() } } }
            };
            var rzpOrder = client.Order.Create(options);

            return Ok(new
            {
                razorpayOrderId = rzpOrder["id"].ToString(),
                amount = amountPaise,
                currency = "INR",
                keyId,
                subTotal, delivery, tax, discount, total
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // ── POST /api/v1/orders/verify-payment ───────────────────────────────────
    [HttpPost("verify-payment")]
    public async Task<IActionResult> VerifyPayment([FromBody] VerifyPaymentRequest req)
    {
        // Verify HMAC-SHA256 signature
        var keySecret = config["Razorpay:KeySecret"]!;
        var payload = $"{req.RazorpayOrderId}|{req.RazorpayPaymentId}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(keySecret));
        var computed = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload))).ToLower();

        if (computed != req.RazorpaySignature)
            return BadRequest(new { error = "Payment verification failed. Invalid signature." });

        // Signature valid — create the order
        var cart = await db.Carts.Include(c => c.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.CustomerId == UserId);
        if (cart == null || !cart.Items.Any()) return BadRequest(new { error = "Cart is empty" });

        // Stock validation
        foreach (var item in cart.Items)
        {
            if (item.Product.StockQuantity < item.Quantity)
                return BadRequest(new { error = $"'{item.Product.Name}' only has {item.Product.StockQuantity} units available." });
        }

        var (subTotal, delivery, tax, discount, coupon) = await CalcTotalsAsync(UserId, req.CouponCode);
        if (coupon != null) coupon.UsedCount++;

        var order = new AppOrder
        {
            CustomerId = UserId,
            DeliveryAddress = req.DeliveryAddress,
            Notes = req.Notes,
            SubTotal = subTotal,
            DeliveryFee = delivery,
            TaxAmount = tax,
            DiscountAmount = discount,
            TotalAmount = Math.Max(0, subTotal + delivery + tax - discount),
            EstimatedDelivery = DateTime.UtcNow.AddDays(2),
            RazorpayOrderId = req.RazorpayOrderId,
            RazorpayPaymentId = req.RazorpayPaymentId,
            Status = "Processing",
            Items = cart.Items.Select(i => new OrderItem
            {
                ProductId = i.ProductId,
                ProductName = i.Product.Name,
                Quantity = i.Quantity,
                UnitPrice = i.Product.DiscountPercent > 0
                    ? Math.Round(i.Product.Price * (1 - i.Product.DiscountPercent / 100m), 2)
                    : i.Product.Price
            }).ToList()
        };

        // Deduct stock
        foreach (var item in cart.Items)
            item.Product.StockQuantity -= item.Quantity;

        db.Orders.Add(order);
        db.CartItems.RemoveRange(cart.Items);
        await db.SaveChangesAsync();

        // Publish order placed event — email + notifications handled by consumer
        var customer = await db.Users.FindAsync(UserId);
        await bus.Publish(new OrderPlacedMessage
        {
            OrderId = order.Id,
            OrderRef = order.Id.ToString()[..8].ToUpper(),
            CustomerId = UserId,
            CustomerEmail = customer?.Email ?? "",
            CustomerFirstName = customer?.FirstName ?? "",
            Total = order.TotalAmount,
            Items = order.Items.Select(i => new OrderItemLine { ProductName = i.ProductName, Quantity = i.Quantity, UnitPrice = i.UnitPrice }).ToList(),
            IsPaid = true
        });

        // Publish stock alerts
        foreach (var item in cart.Items)
            if (item.Product.StockQuantity <= 5)
                await bus.Publish(new StockAlertMessage { ProductName = item.Product.Name, RemainingStock = item.Product.StockQuantity });

        return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, ToDto(order));
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder(CreateOrderRequest req)
    {
        var cart = await db.Carts.Include(c => c.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.CustomerId == UserId);
        if (cart == null || !cart.Items.Any()) return BadRequest(new { error = "Cart is empty" });

        // Stock validation
        foreach (var item in cart.Items)
        {
            if (item.Product.StockQuantity < item.Quantity)
                return BadRequest(new { error = $"'{item.Product.Name}' only has {item.Product.StockQuantity} units available." });
        }

        var subTotal = cart.Items.Sum(i => {
            var unitPrice = i.Product.DiscountPercent > 0
                ? Math.Round(i.Product.Price * (1 - i.Product.DiscountPercent / 100m), 2)
                : i.Product.Price;
            return unitPrice * i.Quantity;
        });
        var deliveryFee = subTotal >= 500 ? 0m : 49m;
        var tax = Math.Round(subTotal * 0.05m, 2);

        decimal discount = 0;
        if (!string.IsNullOrWhiteSpace(req.CouponCode))
        {
            var coupon = await db.Coupons.FirstOrDefaultAsync(c =>
                c.Code == req.CouponCode.ToUpper() && c.IsActive &&
                (c.ExpiresAt == null || c.ExpiresAt > DateTime.UtcNow) &&
                c.UsedCount < c.UsageLimit && subTotal >= c.MinOrderAmount);
            if (coupon != null)
            {
                discount = coupon.DiscountType == "Percentage"
                    ? Math.Round(subTotal * coupon.DiscountValue / 100, 2)
                    : Math.Min(coupon.DiscountValue, subTotal);
                coupon.UsedCount++;
            }
        }

        var order = new AppOrder
        {
            CustomerId = UserId,
            DeliveryAddress = req.DeliveryAddress,
            Notes = req.Notes,
            SubTotal = subTotal,
            DeliveryFee = deliveryFee,
            TaxAmount = tax,
            DiscountAmount = discount,
            TotalAmount = Math.Max(0, subTotal + deliveryFee + tax - discount),
            EstimatedDelivery = DateTime.UtcNow.AddDays(2),
            Items = cart.Items.Select(i => new OrderItem
            {
                ProductId = i.ProductId,
                ProductName = i.Product.Name,
                Quantity = i.Quantity,
                UnitPrice = i.Product.DiscountPercent > 0
                    ? Math.Round(i.Product.Price * (1 - i.Product.DiscountPercent / 100m), 2)
                    : i.Product.Price
            }).ToList()
        };

        // Deduct stock
        foreach (var item in cart.Items)
            item.Product.StockQuantity -= item.Quantity;

        db.Orders.Add(order);
        db.CartItems.RemoveRange(cart.Items);
        await db.SaveChangesAsync();

        // Publish order placed event — email + notifications handled by consumer
        var customer = await db.Users.FindAsync(UserId);
        await bus.Publish(new OrderPlacedMessage
        {
            OrderId = order.Id,
            OrderRef = order.Id.ToString()[..8].ToUpper(),
            CustomerId = UserId,
            CustomerEmail = customer?.Email ?? "",
            CustomerFirstName = customer?.FirstName ?? "",
            Total = order.TotalAmount,
            Items = order.Items.Select(i => new OrderItemLine { ProductName = i.ProductName, Quantity = i.Quantity, UnitPrice = i.UnitPrice }).ToList(),
            IsPaid = false
        });

        // Publish stock alerts
        foreach (var item in cart.Items)
            if (item.Product.StockQuantity <= 5)
                await bus.Publish(new StockAlertMessage { ProductName = item.Product.Name, RemainingStock = item.Product.StockQuantity });

        return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, ToDto(order));
    }

    [HttpPatch("{id}/status")]
    [Authorize(Roles = "Admin,StoreManager,DeliveryDriver")]
    public async Task<IActionResult> UpdateStatus(Guid id, UpdateOrderStatusRequest req)
    {
        var order = await db.Orders.FindAsync(id);
        if (order == null) return NotFound();
        order.Status = req.Status;
        if (req.Status == "Delivered") order.DeliveredAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        // Publish status changed event — email + notifications handled by consumer
        var customer = await db.Users.FindAsync(order.CustomerId);
        await bus.Publish(new OrderStatusChangedMessage
        {
            OrderId = order.Id,
            OrderRef = id.ToString()[..8].ToUpper(),
            CustomerId = order.CustomerId,
            CustomerEmail = customer?.Email ?? "",
            CustomerFirstName = customer?.FirstName ?? "",
            NewStatus = req.Status
        });

        return NoContent();
    }

}
