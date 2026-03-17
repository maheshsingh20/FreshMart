using SharedKernel.Domain;

namespace AuthService.Domain;

public enum UserRole { Customer, Admin, StoreManager, DeliveryDriver }

public class User : AggregateRoot
{
    public string Email { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public string? PhoneNumber { get; private set; }
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; } = true;
    public bool IsEmailVerified { get; private set; }
    public string? RefreshToken { get; private set; }
    public DateTime? RefreshTokenExpiry { get; private set; }

    private User() { } // EF Core

    public static User Create(string email, string passwordHash, string firstName,
        string lastName, UserRole role = UserRole.Customer, string? phone = null)
    {
        var user = new User
        {
            Email = email.ToLowerInvariant(),
            PasswordHash = passwordHash,
            FirstName = firstName,
            LastName = lastName,
            Role = role,
            PhoneNumber = phone
        };
        user.AddDomainEvent(new UserRegisteredEvent(user.Id, user.Email, user.Role.ToString()));
        return user;
    }

    public void SetRefreshToken(string token, DateTime expiry)
    {
        RefreshToken = token;
        RefreshTokenExpiry = expiry;
        SetUpdated();
    }

    public void RevokeRefreshToken()
    {
        RefreshToken = null;
        RefreshTokenExpiry = null;
        SetUpdated();
    }

    public void VerifyEmail() { IsEmailVerified = true; SetUpdated(); }
    public void Deactivate() { IsActive = false; SetUpdated(); }

    public string FullName => $"{FirstName} {LastName}";
}

public record UserRegisteredEvent(Guid UserId, string Email, string Role)
    : DomainEvent
{
    public override string EventType => "UserRegistered";
}
