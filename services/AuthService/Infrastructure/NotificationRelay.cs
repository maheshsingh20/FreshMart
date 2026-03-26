using SharedKernel.Events;
using SharedKernel.Messaging;

namespace AuthService.Infrastructure;

/// <summary>
/// Publishes all notification events via RabbitMQ.
/// </summary>
public class NotificationRelay(IMessageBus bus, ILogger<NotificationRelay> logger) : INotificationRelay
{
    public Task NotifyWelcomeAsync(Guid userId, string email, string firstName, CancellationToken ct = default)
        => Publish("user.registered", new { userId, email, firstName, occurredOn = DateTime.UtcNow }, ct);

    public Task NotifyOtpAsync(Guid userId, string email, string firstName, string otp, string purpose, CancellationToken ct = default)
        => Publish("otp.requested", new OtpRequestedEvent(userId, email, firstName, otp, purpose, DateTime.UtcNow), ct);

    private async Task Publish<T>(string topic, T message, CancellationToken ct) where T : class
    {
        try
        {
            await bus.PublishAsync(message, topic, ct);
            logger.LogInformation("[Notify] Published '{Topic}' via RabbitMQ", topic);
        }
        catch (Exception ex)
        {
            logger.LogWarning("[Notify] RabbitMQ publish failed for '{Topic}': {Message}", topic, ex.Message);
        }
    }
}
