namespace AuthService.Infrastructure;

public interface INotificationRelay
{
    Task NotifyWelcomeAsync(Guid userId, string email, string firstName, CancellationToken ct = default);
    Task NotifyOtpAsync(Guid userId, string email, string firstName, string otp, string purpose, CancellationToken ct = default);
}
