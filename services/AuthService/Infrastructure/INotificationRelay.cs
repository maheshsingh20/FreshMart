namespace AuthService.Infrastructure;

/// <summary>
/// Abstraction for sending notification events from AuthService.
/// Decouples the application layer from the messaging infrastructure so handlers
/// can be tested without a real RabbitMQ connection.
/// </summary>
public interface INotificationRelay
{
    /// <summary>Publishes a welcome event after a new user registers.</summary>
    Task NotifyWelcomeAsync(Guid userId, string email, string firstName, CancellationToken ct = default);

    /// <summary>
    /// Publishes an OTP event so NotificationService can send the verification or reset email.
    /// </summary>
    /// <param name="purpose">"email-verification" or "password-reset"</param>
    Task NotifyOtpAsync(Guid userId, string email, string firstName, string otp, string purpose, CancellationToken ct = default);
}
