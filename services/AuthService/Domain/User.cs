using BCrypt.Net;
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
    public string? GoogleId { get; private set; }
    public string? RefreshToken { get; private set; }
    public DateTime? RefreshTokenExpiry { get; private set; }
    public string? OtpHash { get; private set; }
    public DateTime? OtpExpiry { get; private set; }
    public string? OtpPurpose { get; private set; }

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

    public static User CreateViaGoogle(string email, string firstName, string lastName, string googleId)
    {
        var user = new User
        {
            Email = email.ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()),
            FirstName = firstName,
            LastName = lastName,
            Role = UserRole.Customer,
            GoogleId = googleId,
            IsEmailVerified = true
        };
        user.AddDomainEvent(new UserRegisteredEvent(user.Id, user.Email, user.Role.ToString()));
        return user;
    }

    public void LinkGoogle(string googleId)
    {
        GoogleId = googleId;
        IsEmailVerified = true;
        SetUpdated();
    }
    public void VerifyEmail() { IsEmailVerified = true; OtpHash = null; OtpExpiry = null; OtpPurpose = null; SetUpdated(); }
    public void Deactivate() { IsActive = false; SetUpdated(); }

    public string GenerateOtp(string purpose)
    {
        var otp = Random.Shared.Next(100000, 999999).ToString();
        OtpHash = BCrypt.Net.BCrypt.HashPassword(otp);
        OtpExpiry = DateTime.UtcNow.AddMinutes(10);
        OtpPurpose = purpose;
        SetUpdated();
        return otp;
    }

    public bool ValidateOtp(string otp, string purpose)
    {
        if (OtpPurpose != purpose) return false;
        if (OtpExpiry == null || OtpExpiry < DateTime.UtcNow) return false;
        if (OtpHash == null || !BCrypt.Net.BCrypt.Verify(otp, OtpHash)) return false;
        OtpHash = null; OtpExpiry = null; OtpPurpose = null;
        SetUpdated();
        return true;
    }

    public void ResetPassword(string newPasswordHash) { PasswordHash = newPasswordHash; SetUpdated(); }

    public void UpdateProfile(string firstName, string lastName, string? phoneNumber)
    {
        FirstName = firstName;
        LastName = lastName;
        PhoneNumber = phoneNumber;
        SetUpdated();
    }

    public string FullName => $"{FirstName} {LastName}";
}

public record UserRegisteredEvent(Guid UserId, string Email, string Role)
    : DomainEvent
{
    public override string EventType => "UserRegistered";
}
