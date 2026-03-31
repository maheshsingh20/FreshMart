using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderService.Application.Commands;
using OrderService.Application.Queries;
using OrderService.Infrastructure;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Net.Http.Json;

namespace OrderService.API.Controllers;

[ApiController]
[Route("api/v1/orders")]
[Authorize]
public class OrdersController(
    IMediator mediator,
    IHttpClientFactory httpClientFactory,
    IConfiguration config,
    NotificationRelay notificationRelay) : ControllerBase
{
    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private string CurrentUserEmail =>
        User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email) ?? "";

    private string CurrentUserFirstName =>
        User.FindFirstValue("firstName") ?? "";

    [HttpGet]
    public async Task<IActionResult> GetMyOrders(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetCustomerOrdersQuery(CurrentUserId, page, pageSize), ct);
        return Ok(result);
    }

    [HttpGet("all")]
    [Authorize(Roles = "Admin,StoreManager")]
    public async Task<IActionResult> GetAllOrders(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 100, CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetAllOrdersQuery(page, pageSize), ct);
        return Ok(result);
    }

    [HttpGet("driver/stats")]
    [Authorize(Roles = "Admin,StoreManager,DeliveryDriver")]
    public async Task<IActionResult> GetDriverStats(CancellationToken ct)
    {
        var result = await mediator.Send(new GetDriverStatsQuery(), ct);
        return Ok(new
        {
            deliveredToday = result.DeliveredToday,
            pending = result.Pending,
            outForDelivery = result.OutForDelivery,
            totalDelivered = result.TotalDelivered
        });
    }

    [HttpGet("driver")]
    [Authorize(Roles = "Admin,StoreManager,DeliveryDriver")]
    public async Task<IActionResult> GetDriverOrders(CancellationToken ct)
    {
        var statuses = new[] { "Processing", "Shipped", "OutForDelivery", "Delivered" };
        var all = new List<OrderDto>();
        foreach (var s in statuses)
        {
            var orders = await mediator.Send(new GetOrdersByStatusQuery(s), ct);
            all.AddRange(orders);
        }
        return Ok(new { orders = all.OrderByDescending(o => o.CreatedAt), total = all.Count });
    }    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetOrder(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetOrderByIdQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest req, CancellationToken ct)
    {
        var cmd = new CreateOrderCommand(CurrentUserId, req.DeliveryAddress,
            req.Items.Select(i => new OrderItemRequest(i.ProductId, i.ProductName, i.Quantity, i.UnitPrice)).ToList(),
            req.Notes);
        var result = await mediator.Send(cmd, ct);
        if (!result.IsSuccess) return BadRequest(new { result.Error });
        return CreatedAtAction(nameof(GetOrder), new { id = result.Value }, new { id = result.Value });
    }

    [HttpPut("{id:guid}/cancel")]
    public async Task<IActionResult> CancelOrder(Guid id, [FromBody] CancelRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(new CancelOrderCommand(id, req.Reason), ct);
        if (!result.IsSuccess) return BadRequest(new { result.Error });
        return NoContent();
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Admin,StoreManager,DeliveryDriver")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateStatusRequest req, CancellationToken ct)
    {
        // Fetch order first to get customer details (admin is the caller, not the customer)
        var order = await mediator.Send(new GetOrderByIdQuery(id), ct);
        if (order is null) return NotFound();

        var result = await mediator.Send(new UpdateOrderStatusCommand(id, req.Status), ct);
        if (!result.IsSuccess) return BadRequest(new { result.Error });

        // Pass full order details so NotificationService can build a rich invoice email on Delivered
        _ = notificationRelay.NotifyOrderStatusChangedAsync(
            order.CustomerId, order.CustomerEmail, order.CustomerFirstName,
            id, id.ToString()[..8].ToUpper(), req.Status,
            deliveryAddress: order.DeliveryAddress,
            totalAmount: order.TotalAmount,
            deliveryFee: order.DeliveryFee,
            taxAmount: order.TaxAmount,
            discountAmount: order.DiscountAmount,
            items: order.Items.Select(i => (i.ProductName, i.Quantity, i.UnitPrice)),
            ct: ct);

        return NoContent();
    }

    private async Task NotifyStatusChangedAsync(Guid orderId, string newStatus, CancellationToken ct)
    {
        try
        {
            var notifUrl = config["Services:NotificationService"] ?? "http://localhost:5007";
            var client = httpClientFactory.CreateClient();
            var orderRef = orderId.ToString()[..8].ToUpper();

            await client.PostAsJsonAsync($"{notifUrl}/api/v1/notifications/order-status-changed", new
            {
                customerId = CurrentUserId,
                customerEmail = CurrentUserEmail,
                customerFirstName = CurrentUserFirstName,
                orderId,
                orderRef,
                newStatus
            }, ct);
        }
        catch (Exception ex)
        {
            var logger = HttpContext.RequestServices.GetRequiredService<ILogger<OrdersController>>();
            logger.LogWarning("Failed to notify status change: {Message}", ex.Message);
        }
    }

    [HttpGet("status/{status}")]
    [Authorize(Roles = "Admin,StoreManager,DeliveryDriver")]
    public async Task<IActionResult> GetByStatus(string status, CancellationToken ct)
    {
        var result = await mediator.Send(new GetOrdersByStatusQuery(status), ct);
        return Ok(result);
    }

    // Proxy: create Razorpay order via PaymentService
    [HttpPost("create-payment")]
    public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentRequest req, CancellationToken ct)
    {
        var paymentServiceUrl = config["Services:PaymentService"] ?? "http://localhost:5005";
        var client = httpClientFactory.CreateClient();

        // Forward the Authorization header
        var token = Request.Headers["Authorization"].ToString();
        if (!string.IsNullOrEmpty(token))
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", token);

        var body = new
        {
            orderId = Guid.Empty,   // placeholder — payment created before order
            customerId = CurrentUserId,
            amount = req.Amount,
            method = 0              // 0 = Online/Razorpay
        };

        var response = await client.PostAsJsonAsync($"{paymentServiceUrl}/api/v1/payments", body, ct);
        var content = await response.Content.ReadFromJsonAsync<object>(ct);
        if (!response.IsSuccessStatusCode) return StatusCode((int)response.StatusCode, content);
        return Ok(content);
    }

    // Proxy: verify payment + create order in one step
    [HttpPost("verify-payment")]
    public async Task<IActionResult> VerifyPayment([FromBody] VerifyAndCreateOrderRequest req, CancellationToken ct)
    {
        var paymentServiceUrl = config["Services:PaymentService"] ?? "http://localhost:5005";
        var client = httpClientFactory.CreateClient();

        var token = Request.Headers["Authorization"].ToString();
        if (!string.IsNullOrEmpty(token))
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", token);

        // Step 1: verify signature
        var verifyBody = new
        {
            razorpayOrderId = req.RazorpayOrderId,
            razorpayPaymentId = req.RazorpayPaymentId,
            signature = req.RazorpaySignature
        };
        var verifyResponse = await client.PostAsJsonAsync($"{paymentServiceUrl}/api/v1/payments/verify", verifyBody, ct);
        if (!verifyResponse.IsSuccessStatusCode)
        {
            var err = await verifyResponse.Content.ReadFromJsonAsync<object>(ct);
            return StatusCode((int)verifyResponse.StatusCode, err);
        }

        // Step 2: create the order record
        var cart = req.Items is { Count: > 0 }
            ? req.Items
            : new List<OrderItemRequest>();

        var cmd = new CreateOrderCommand(CurrentUserId, req.DeliveryAddress, cart, req.Notes,
            CurrentUserEmail, CurrentUserFirstName);
        var result = await mediator.Send(cmd, ct);
        if (!result.IsSuccess) return BadRequest(new { result.Error });

        // Step 3: notify — RabbitMQ first, HTTP fallback
        var orderRef = result.Value.ToString()[..8].ToUpper();
        var total = req.Items?.Sum(i => i.Quantity * i.UnitPrice) ?? 0;
        _ = notificationRelay.NotifyOrderCreatedAsync(
            CurrentUserId, CurrentUserEmail, CurrentUserFirstName,
            result.Value, orderRef, total,
            req.Items?.Select(i => (i.ProductName, i.Quantity, i.UnitPrice)) ?? [],
            ct);

        return Ok(new { orderId = result.Value, message = "Order placed successfully" });
    }

    private async Task NotifyOrderCreatedAsync(Guid orderId, VerifyAndCreateOrderRequest req, CancellationToken ct)
    {
        try
        {
            var notifUrl = config["Services:NotificationService"] ?? "http://localhost:5007";
            var client = httpClientFactory.CreateClient();
            var orderRef = orderId.ToString()[..8].ToUpper();
            var total = req.Items?.Sum(i => i.Quantity * i.UnitPrice) ?? 0;

            await client.PostAsJsonAsync($"{notifUrl}/api/v1/notifications/order-created", new
            {
                customerId = CurrentUserId,
                customerEmail = CurrentUserEmail,
                customerFirstName = CurrentUserFirstName,
                orderId,
                orderRef,
                total,
                items = req.Items?.Select(i => new { i.ProductName, i.Quantity, i.UnitPrice }) ?? []
            }, ct);
        }
        catch (Exception ex)
        {
            // Non-critical — log and continue
            var logger = HttpContext.RequestServices.GetRequiredService<ILogger<OrdersController>>();
            logger.LogWarning("Failed to notify NotificationService: {Message}", ex.Message);
        }
    }
}

public record CreateOrderRequest(string DeliveryAddress, List<OrderItemRequest> Items, string? Notes);
public record CancelRequest(string Reason);
public record UpdateStatusRequest(string Status);
public record CreatePaymentRequest(decimal Amount);
public record VerifyAndCreateOrderRequest(
    string RazorpayOrderId,
    string RazorpayPaymentId,
    string RazorpaySignature,
    string DeliveryAddress,
    string? Notes,
    string? CouponCode,
    List<OrderItemRequest>? Items);
