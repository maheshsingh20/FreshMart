using BCrypt.Net;
using SharedKernel.Domain;

namespace AuthService.Domain;

/// <summary>
/// Roles that determine what a user can access across the platform.
/// Stored as a string in the database for readability in queries.
/// </summary>
public enum UserRole { Customer, Admin, StoreManager, DeliveryDriver }

/// <summary>
/// The central aggregate root for authentication and identity.
/// Owns all auth-related state: credentials, tokens, OTPs, and Google SSO linkage.
/// All mutations go through domain methods to keep invariants enforced in one place.
/// </summary>
public class User : AggregateRoot
{
    /// <summary>Normalised (lowercase) email address — unique across the platform.</summary>
    public string Email { get; private set; } = default!;

    /// <summary>BCrypt hash of the user's password. Never exposed outside the domain.</summary>
    public string PasswordHash { get; private set; } = default!;

    /// <summary>User's given name, used in personalised notifications and the UI.</summary>
    public string FirstName { get; private set; } = default!;

    /// <summary>User's family name.</summary>
    public string LastName { get; private set; } = default!;

    /// <summary>Optional contact number for delivery coordination.</summary>
    public string? PhoneNumber { get; private set; }

    /// <summary>Determines which dashboard and permissions the user receives.</summary>
    public UserRole Role { get; private set; }

    /// <summary>Soft-delete flag — deactivated users cannot log in but their data is retained.</summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>Whether the user has confirmed ownership of their email via OTP.</summary>
    public bool IsEmailVerified { get; private set; }

    /// <summary>Google OAuth subject identifier, set when the user signs in via Google.</summary>
    public string? GoogleId { get; private set; }

    /// <summary>Opaque refresh token stored server-side to enable token rotation.</summary>
    public string? RefreshToken { get; private set; }

    /// <summary>UTC expiry of the current refresh token; null means no active session.</summary>
    public DateTime? RefreshTokenExpiry { get; private set; }

    /// <summary>BCrypt hash of the most recently generated OTP. Cleared after use or expiry.</summary>
    public string? OtpHash { get; private set; }

    /// <summary>UTC expiry of the current OTP — OTPs are valid for 10 minutes.</summary>
    public DateTime? OtpExpiry { get; private set; }

    /// <summary>Distinguishes the OTP's intended use: "email-verification" or "password-reset".</summary>
    public string? OtpPurpose { get; private set; }

    /// <summary>Required by EF Core — not for direct use.</summary>
    private User() { }

    /// <summary>
    /// Factory method for standard email/password registration.
    /// Raises <see cref="UserRegisteredEvent"/> so downstream services (e.g. NotificationService) react.
    /// </summary>
    /// <param name="email">Raw email — normalised to lowercase internally.</param>
    /// <param name="passwordHash">Pre-hashed password from <see cref="IPasswordHasher"/>.</param>
    /// <param name="firstName">User's given name.</param>
    /// <param name="lastName">User's family name.</param>
    /// <param name="role">Assigned role; defaults to <see cref="UserRole.Customer"/>.</param>
    /// <param name="phone">Optional phone number.</param>
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

    /// <summary>
    /// Stores a new refresh token after successful login or token rotation.
    /// The previous token is implicitly invalidated by overwriting it.
    /// </summary>
    /// <param name="token">The new opaque refresh token.</param>
    /// <param name="expiry">UTC expiry date (typically 30 days from now).</param>
    public void SetRefreshToken(string token, DateTime expiry)
    {
        RefreshToken = token;
        RefreshTokenExpiry = expiry;
        SetUpdated();
    }

    /// <summary>
    /// Clears the refresh token on logout, preventing further token refreshes.
    /// </summary>
    public void RevokeRefreshToken()
    {
        RefreshToken = null;
        RefreshTokenExpiry = null;
        SetUpdated();
    }

    /// <summary>
    /// Factory method for Google OAuth sign-in.
    /// Email is considered verified because Google has already confirmed it.
    /// A random password hash is set so the account cannot be used for password login.
    /// </summary>
    /// <param name="email">Email from the Google ID token payload.</param>
    /// <param name="firstName">Given name from the Google profile.</param>
    /// <param name="lastName">Family name from the Google profile.</param>
    /// <param name="googleId">Google subject identifier ("sub" claim).</param>
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

    /// <summary>
    /// Links an existing email/password account to a Google identity.
    /// Called when a user who registered via email later signs in with Google using the same address.
    /// </summary>
    /// <param name="googleId">Google subject identifier to associate with this account.</param>
    public void LinkGoogle(string googleId)
    {
        GoogleId = googleId;
        IsEmailVerified = true;
        SetUpdated();
    }

    /// <summary>
    /// Marks the email as verified and clears the consumed OTP fields.
    /// </summary>
    public void VerifyEmail() { IsEmailVerified = true; OtpHash = null; OtpExpiry = null; OtpPurpose = null; SetUpdated(); }

    /// <summary>Soft-deactivates the account. The user can no longer log in.</summary>
    public void Deactivate() { IsActive = false; SetUpdated(); }

    /// <summary>
    /// Generates a 6-digit OTP, hashes it with BCrypt, and stores it with a 10-minute expiry.
    /// Returns the plain-text OTP so it can be sent to the user — it is never stored in plain text.
    /// </summary>
    /// <param name="purpose">"email-verification" or "password-reset"</param>
    /// <returns>The plain-text OTP to be delivered to the user.</returns>
    public string GenerateOtp(string purpose)
    {
        var otp = Random.Shared.Next(100000, 999999).ToString();
        OtpHash = BCrypt.Net.BCrypt.HashPassword(otp);
        OtpExpiry = DateTime.UtcNow.AddMinutes(10);
        OtpPurpose = purpose;
        SetUpdated();
        return otp;
    }

    /// <summary>
    /// Validates the supplied OTP against the stored hash, expiry, and purpose.
    /// Clears the OTP fields on success to prevent replay attacks.
    /// </summary>
    /// <param name="otp">The plain-text OTP submitted by the user.</param>
    /// <param name="purpose">Expected purpose — must match the purpose the OTP was generated for.</param>
    /// <returns><c>true</c> if the OTP is valid, unexpired, and matches the expected purpose.</returns>
    public bool ValidateOtp(string otp, string purpose)
    {
        if (OtpPurpose != purpose) return false;
        if (OtpExpiry == null || OtpExpiry < DateTime.UtcNow) return false;
        if (OtpHash == null || !BCrypt.Net.BCrypt.Verify(otp, OtpHash)) return false;
        OtpHash = null; OtpExpiry = null; OtpPurpose = null;
        SetUpdated();
        return true;
    }

    /// <summary>Replaces the password hash after a successful password-reset OTP flow.</summary>
    /// <param name="newPasswordHash">BCrypt hash of the new password.</param>
    public void ResetPassword(string newPasswordHash) { PasswordHash = newPasswordHash; SetUpdated(); }

    /// <summary>Updates mutable profile fields. Email and role cannot be changed here.</summary>
    /// <param name="firstName">New given name.</param>
    /// <param name="lastName">New family name.</param>
    /// <param name="phoneNumber">New phone number, or <c>null</c> to clear it.</param>
    public void UpdateProfile(string firstName, string lastName, string? phoneNumber)
    {
        FirstName = firstName;
        LastName = lastName;
        PhoneNumber = phoneNumber;
        SetUpdated();
    }

    /// <summary>Convenience property combining first and last name for display purposes.</summary>
    public string FullName => $"{FirstName} {LastName}";
}

/// <summary>
/// Domain event raised when a new user account is created (via email or Google).
/// Consumed in-process by MediatR handlers and published to RabbitMQ for cross-service notification.
/// </summary>
/// <param name="UserId">The newly created user's identifier.</param>
/// <param name="Email">The user's email address.</param>
/// <param name="Role">The assigned role as a string.</param>
public record UserRegisteredEvent(Guid UserId, string Email, string Role)
    : DomainEvent
{
    /// <inheritdoc/>
    public override string EventType => "UserRegistered";
}
