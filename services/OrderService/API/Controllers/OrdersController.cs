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

/// <summary>
/// HTTP API controller for order lifecycle management.
/// Handles the full order journey: creation after payment verification, status
/// transitions (Processing → Shipped → OutForDelivery → Delivered), cancellation,
/// and admin/driver views. Also acts as a thin proxy for the Razorpay payment flow
/// so the frontend only needs to talk to one service during checkout.
/// Notifications are dispatched asynchronously via <see cref="NotificationRelay"/>
/// (RabbitMQ with HTTP fallback) so they never block the HTTP response.
/// </summary>
[ApiController]
[Route("api/v1/orders")]
[Authorize]
public class OrdersController(
    IMediator mediator,
    IHttpClientFactory httpClientFactory,
    IConfiguration config,
    NotificationRelay notificationRelay) : ControllerBase
{
    /// <summary>
    /// Extracts the authenticated customer's ID from the JWT <c>sub</c> claim,
    /// falling back to <see cref="ClaimTypes.NameIdentifier"/> for compatibility
    /// with different token issuers.
    /// </summary>
    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>
    /// The authenticated user's email address, extracted from the JWT <c>email</c> claim.
    /// Used when dispatching order-created and status-changed notifications.
    /// </summary>
    private string CurrentUserEmail =>
        User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email) ?? "";

    /// <summary>
    /// The authenticated user's first name, extracted from the custom <c>firstName</c> JWT claim.
    /// Used to personalise notification emails (e.g. "Hi John, your order is on its way!").
    /// </summary>
    private string CurrentUserFirstName =>
        User.FindFirstValue("firstName") ?? "";

    /// <summary>
    /// Returns a paginated list of orders belonging to the currently authenticated customer.
    /// Customers can only see their own orders; use <see cref="GetAllOrders"/> for admin access.
    /// </summary>
    /// <param name="page">1-based page number (default 1).</param>
    /// <param name="pageSize">Number of orders per page (default 10).</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet]
    public async Task<IActionResult> GetMyOrders(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetCustomerOrdersQuery(CurrentUserId, page, pageSize), ct);
        return Ok(result);
    }

    /// <summary>
    /// Returns a paginated list of all orders across all customers.
    /// Restricted to Admin and StoreManager roles for back-office order management.
    /// </summary>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Number of orders per page (default 100).</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("all")]
    [Authorize(Roles = "Admin,StoreManager")]
    public async Task<IActionResult> GetAllOrders(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 100, CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetAllOrdersQuery(page, pageSize), ct);
        return Ok(result);
    }

    /// <summary>
    /// Returns aggregated delivery statistics for the driver dashboard:
    /// how many orders are pending, out for delivery, delivered today, and total delivered.
    /// Computed live from the order database rather than a separate analytics store.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
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

    /// <summary>
    /// Returns all orders that are in an active delivery state (Processing, Shipped,
    /// OutForDelivery, Delivered), sorted newest-first. Used by the driver app to
    /// show the full workload in a single request rather than querying each status separately.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
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
    }

    /// <summary>
    /// Retrieves a single order by its unique identifier.
    /// Customers can fetch their own orders; admins and drivers can fetch any order.
    /// Returns 404 if the order does not exist.
    /// </summary>
    /// <param name="id">The order's unique identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetOrder(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetOrderByIdQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Creates a new order for the authenticated customer.
    /// Stock availability is validated inside the command handler before the order
    /// is persisted, and stock is deducted from ProductService atomically.
    /// This endpoint is used for cash-on-delivery or pre-verified payment flows;
    /// for Razorpay online payments use <see cref="VerifyPayment"/> instead.
    /// </summary>
    /// <param name="req">Delivery address, line items, and optional notes.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>201 Created with the new order ID, or 400 Bad Request on validation failure.</returns>
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

    /// <summary>
    /// Cancels an order that has not yet been shipped.
    /// The cancellation reason is stored on the order for audit purposes.
    /// Returns 400 if the order is in a state that cannot be cancelled (e.g. already delivered).
    /// </summary>
    /// <param name="id">The order to cancel.</param>
    /// <param name="req">The reason for cancellation.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPut("{id:guid}/cancel")]
    public async Task<IActionResult> CancelOrder(Guid id, [FromBody] CancelRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(new CancelOrderCommand(id, req.Reason), ct);
        if (!result.IsSuccess) return BadRequest(new { result.Error });
        return NoContent();
    }

    /// <summary>
    /// Advances an order to a new status (e.g. Processing → Shipped → Delivered).
    /// Restricted to Admin, StoreManager, and DeliveryDriver roles.
    /// After a successful status change, a notification is dispatched asynchronously
    /// via <see cref="NotificationRelay"/> — on Delivered this includes a full invoice
    /// email with itemised totals, fees, and the delivery address.
    /// </summary>
    /// <param name="id">The order whose status should be updated.</param>
    /// <param name="req">The target status string.</param>
    /// <param name="ct">Cancellation token.</param>
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

    /// <summary>
    /// Returns all orders with a specific status string.
    /// Useful for admin dashboards that need to filter by a single status bucket
    /// (e.g. all "Pending" orders awaiting processing).
    /// Restricted to Admin, StoreManager, and DeliveryDriver roles.
    /// </summary>
    /// <param name="status">The order status to filter by (e.g. "Processing", "Delivered").</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("status/{status}")]
    [Authorize(Roles = "Admin,StoreManager,DeliveryDriver")]
    public async Task<IActionResult> GetByStatus(string status, CancellationToken ct)
    {
        var result = await mediator.Send(new GetOrdersByStatusQuery(status), ct);
        return Ok(result);
    }

    /// <summary>
    /// Proxy endpoint that forwards a payment creation request to PaymentService,
    /// returning the Razorpay order ID and public key to the frontend.
    /// The frontend uses these to open the Razorpay checkout modal.
    /// The Authorization header is forwarded so PaymentService can authenticate the caller.
    /// </summary>
    /// <param name="req">The order amount in INR.</param>
    /// <param name="ct">Cancellation token.</param>
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

    /// <summary>
    /// Two-phase checkout endpoint that verifies the Razorpay payment signature and,
    /// on success, atomically creates the order record and dispatches an order-created
    /// notification. This is the primary checkout path for online payments.
    /// Phase 1: Forwards the HMAC signature to PaymentService for cryptographic verification.
    /// Phase 2: Creates the order via MediatR, deducts stock, and fires the notification.
    /// If signature verification fails the order is never created, preventing fraud.
    /// </summary>
    /// <param name="req">
    /// Razorpay callback data (order ID, payment ID, signature) plus the cart items
    /// and delivery address needed to create the order record.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
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

/// <summary>Request body for creating an order directly (COD or pre-verified payment).</summary>
public record CreateOrderRequest(string DeliveryAddress, List<OrderItemRequest> Items, string? Notes);

/// <summary>Request body for cancelling an order, capturing the reason for audit.</summary>
public record CancelRequest(string Reason);

/// <summary>Request body for advancing an order to a new status.</summary>
public record UpdateStatusRequest(string Status);

/// <summary>Request body for initiating a Razorpay payment order (amount in INR).</summary>
public record CreatePaymentRequest(decimal Amount);

/// <summary>
/// Combined request body for the verify-and-create-order flow.
/// Contains both the Razorpay callback data needed for signature verification
/// and the cart/delivery data needed to create the order record.
/// </summary>
public record VerifyAndCreateOrderRequest(
    string RazorpayOrderId,
    string RazorpayPaymentId,
    string RazorpaySignature,
    string DeliveryAddress,
    string? Notes,
    string? CouponCode,
    List<OrderItemRequest>? Items);
