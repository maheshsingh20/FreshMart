using SharedKernel.Events;
using SharedKernel.Messaging;
using System.Net.Http.Json;

namespace OrderService.Infrastructure;

/// <summary>
/// Resilient notification dispatcher used by the OrderService to inform customers
/// about order lifecycle events (order created, status changed).
/// Implements a two-tier delivery strategy:
/// <list type="number">
///   <item>
///     <b>Primary:</b> Publishes a strongly-typed domain event to RabbitMQ via
///     <see cref="IEventPublisher"/>. The NotificationService consumes these events
///     asynchronously, decoupling the two services.
///   </item>
///   <item>
///     <b>Fallback:</b> If RabbitMQ is unavailable (broker down, network partition),
///     falls back to a direct HTTP POST to the NotificationService REST API.
///   </item>
/// </list>
/// This pattern ensures notifications are delivered even during infrastructure
/// degradation, at the cost of at-most-once delivery semantics on the HTTP path.
/// </summary>
public class NotificationRelay(
    IEventPublisher events,
    IHttpClientFactory httpClientFactory,
    IConfiguration config,
    ILogger<NotificationRelay> logger)
{
    /// <summary>Base URL of the NotificationService, read from configuration.</summary>
    private readonly string _notifUrl = config["Services:NotificationService"] ?? "http://localhost:5007";

    /// <summary>
    /// Notifies the customer that their order has been successfully placed.
    /// Builds an <see cref="OrderCreatedEvent"/> with the full item list so the
    /// NotificationService can render a detailed order confirmation email.
    /// </summary>
    /// <param name="customerId">The customer who placed the order.</param>
    /// <param name="customerEmail">Destination email address for the confirmation.</param>
    /// <param name="customerFirstName">Used to personalise the email greeting.</param>
    /// <param name="orderId">The newly created order's ID.</param>
    /// <param name="orderRef">Short human-readable reference (first 8 chars of the ID, uppercased).</param>
    /// <param name="total">Grand total of the order in INR.</param>
    /// <param name="items">Line items to include in the confirmation email.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task NotifyOrderCreatedAsync(
        Guid customerId, string customerEmail, string customerFirstName,
        Guid orderId, string orderRef, decimal total,
        IEnumerable<(string ProductName, int Quantity, decimal UnitPrice)> items,
        CancellationToken ct = default)
    {
        var evt = new OrderCreatedEvent(
            orderId, customerId, total,
            items.Select(i => new OrderItemDto(Guid.Empty, i.ProductName, i.Quantity, i.UnitPrice)).ToList(),
            DateTime.UtcNow)
        {
            CustomerEmail = customerEmail,
            CustomerFirstName = customerFirstName,
            OrderRef = orderRef
        };

        var published = await TryPublishAsync(evt, ct);
        if (!published)
            await HttpFallbackAsync("order-created", new
            {
                customerId, customerEmail, customerFirstName,
                orderId, orderRef, total,
                items = items.Select(i => new { i.ProductName, i.Quantity, i.UnitPrice })
            }, ct);
    }

    /// <summary>
    /// Notifies the customer that their order's status has changed.
    /// When the new status is "Delivered", the full financial breakdown
    /// (total, delivery fee, tax, discount) and item list are included so the
    /// NotificationService can generate a rich invoice email.
    /// </summary>
    /// <param name="customerId">The customer who owns the order.</param>
    /// <param name="customerEmail">Destination email address.</param>
    /// <param name="customerFirstName">Used to personalise the email greeting.</param>
    /// <param name="orderId">The order whose status changed.</param>
    /// <param name="orderRef">Short human-readable reference.</param>
    /// <param name="newStatus">The new status string (e.g. "Shipped", "Delivered").</param>
    /// <param name="deliveryAddress">The delivery address, included in the invoice email.</param>
    /// <param name="totalAmount">Grand total, used in the invoice email.</param>
    /// <param name="deliveryFee">Delivery fee component of the total.</param>
    /// <param name="taxAmount">Tax component of the total.</param>
    /// <param name="discountAmount">Discount applied to the order.</param>
    /// <param name="items">Line items, included in the invoice email on delivery.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task NotifyOrderStatusChangedAsync(
        Guid customerId, string customerEmail, string customerFirstName,
        Guid orderId, string orderRef, string newStatus,
        string deliveryAddress = "",
        decimal totalAmount = 0, decimal deliveryFee = 0, decimal taxAmount = 0, decimal discountAmount = 0,
        IEnumerable<(string ProductName, int Quantity, decimal UnitPrice)>? items = null,
        CancellationToken ct = default)
    {
        var evt = new OrderStatusChangedEvent(
            orderId, customerId, orderRef, newStatus,
            customerEmail, customerFirstName, DateTime.UtcNow,
            deliveryAddress, totalAmount, deliveryFee, taxAmount, discountAmount,
            items?.Select(i => new OrderItemDto(Guid.Empty, i.ProductName, i.Quantity, i.UnitPrice)).ToList());

        var published = await TryPublishAsync(evt, ct);
        if (!published)
            await HttpFallbackAsync("order-status-changed", new
            {
                customerId, customerEmail, customerFirstName,
                orderId, orderRef, newStatus
            }, ct);
    }

    /// <summary>
    /// Attempts to publish a domain event to RabbitMQ.
    /// Returns <c>true</c> on success, <c>false</c> if the broker is unavailable.
    /// Exceptions are caught and logged so the caller can decide whether to fall back.
    /// </summary>
    /// <typeparam name="T">The event type to publish.</typeparam>
    /// <param name="evt">The event payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>true</c> if published successfully; <c>false</c> otherwise.</returns>
    private async Task<bool> TryPublishAsync<T>(T evt, CancellationToken ct) where T : class
    {
        try
        {
            await events.PublishAsync(evt, ct);
            logger.LogInformation("[Notify] Published {EventType} via RabbitMQ", typeof(T).Name);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning("[Notify] RabbitMQ publish failed for {EventType}: {Message} — falling back to HTTP", typeof(T).Name, ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Sends a notification payload directly to the NotificationService REST API.
    /// Used as a fallback when RabbitMQ is unavailable. Errors are logged but not
    /// re-thrown — notification delivery is best-effort and must not fail the
    /// primary order operation.
    /// </summary>
    /// <param name="endpoint">The NotificationService endpoint suffix (e.g. "order-created").</param>
    /// <param name="payload">The anonymous object to serialise as the request body.</param>
    /// <param name="ct">Cancellation token.</param>
    private async Task HttpFallbackAsync(string endpoint, object payload, CancellationToken ct)
    {
        try
        {
            var client = httpClientFactory.CreateClient();
            var response = await client.PostAsJsonAsync($"{_notifUrl}/api/v1/notifications/{endpoint}", payload, ct);
            logger.LogInformation("[Notify] HTTP fallback to {Endpoint} — {Status}", endpoint, response.StatusCode);
        }
        catch (Exception ex)
        {
            logger.LogError("[Notify] HTTP fallback also failed for {Endpoint}: {Message}", endpoint, ex.Message);
        }
    }
}
