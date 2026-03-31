using SharedKernel.Events;
using SharedKernel.Messaging;
using System.Net.Http.Json;

namespace OrderService.Infrastructure;

/// <summary>
/// Publishes notification events via RabbitMQ first.
/// Falls back to direct HTTP call to NotificationService if RabbitMQ is unavailable.
/// </summary>
public class NotificationRelay(
    IEventPublisher events,
    IHttpClientFactory httpClientFactory,
    IConfiguration config,
    ILogger<NotificationRelay> logger)
{
    private readonly string _notifUrl = config["Services:NotificationService"] ?? "http://localhost:5007";

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
