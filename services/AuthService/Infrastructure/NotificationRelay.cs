using SharedKernel.Events;
using SharedKernel.Messaging;

namespace AuthService.Infrastructure;

/// <summary>
/// RabbitMQ implementation of <see cref="INotificationRelay"/>.
/// Publishes notification events fire-and-forget — failures are logged but never propagate
/// to the caller so a messaging outage never breaks the auth flow.
/// </summary>
public class NotificationRelay(IMessageBus bus, ILogger<NotificationRelay> logger) : INotificationRelay
{
    /// <inheritdoc/>
    public Task NotifyWelcomeAsync(Guid userId, string email, string firstName, CancellationToken ct = default)
        => Publish("user.registered", new { userId, email, firstName, occurredOn = DateTime.UtcNow }, ct);

    /// <inheritdoc/>
    public Task NotifyOtpAsync(Guid userId, string email, string firstName, string otp, string purpose, CancellationToken ct = default)
        => Publish("otp.requested", new OtpRequestedEvent(userId, email, firstName, otp, purpose, DateTime.UtcNow), ct);

    /// <summary>
    /// Publishes a message to RabbitMQ and swallows any exception, logging a warning instead.
    /// All notification publishing is fire-and-forget — the auth flow must not fail due to messaging issues.
    /// </summary>
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
