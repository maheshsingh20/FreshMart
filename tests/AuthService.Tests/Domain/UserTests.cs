using AuthService.Domain;
using FluentAssertions;
using NUnit.Framework;

namespace AuthService.Tests.Domain;

/// <summary>
/// Unit tests for the <see cref="User"/> domain entity.
///
/// PURPOSE:
///   Validates all business rules and invariants that live inside the User
///   aggregate root — email normalization, OTP lifecycle, email verification,
///   account deactivation, and computed properties.
///
/// TOOLS USED:
///   - NUnit       : test framework ([TestFixture], [Test])
///   - FluentAssertions : readable assertions (.Should().Be(), etc.)
///
/// NO MOCKS NEEDED:
///   User is a pure domain object with zero external dependencies.
///   Every test creates a User directly and asserts its state — no database,
///   no network, no DI container required.
///
/// TEST NAMING CONVENTION:
///   MethodUnderTest_Scenario_ExpectedBehaviour
///   e.g. Create_ShouldNormalizeEmailToLowercase
/// </summary>
[TestFixture]
public class UserTests
{
    // ── Create ────────────────────────────────────────────────────────────────
    // Tests for the User.Create() factory method.
    // This is the only public way to construct a User (constructor is private
    // for EF Core only). All business rules applied at creation are tested here.

    /// <summary>
    /// Emails must be stored in lowercase so that "John@Example.COM" and
    /// "john@example.com" are treated as the same account.
    /// Verifies that User.Create() calls .ToLowerInvariant() on the email.
    /// </summary>
    [Test]
    public void Create_ShouldNormalizeEmailToLowercase()
    {
        // Arrange — pass an uppercase email
        // Act — create the user
        var user = User.Create("TEST@EXAMPLE.COM", "hash", "John", "Doe");

        // Assert — stored email must be lowercase
        user.Email.Should().Be("test@example.com");
    }

    /// <summary>
    /// When no role is specified, the user should default to Customer.
    /// This prevents accidental privilege escalation on registration.
    /// </summary>
    [Test]
    public void Create_ShouldDefaultRoleToCustomer()
    {
        var user = User.Create("a@b.com", "hash", "John", "Doe");

        // Default role must be Customer, not Admin or StoreManager
        user.Role.Should().Be(UserRole.Customer);
    }

    /// <summary>
    /// Creating a user must raise a UserRegisteredEvent domain event.
    /// This event is consumed by the NotificationService via RabbitMQ
    /// to send a welcome email. If the event is missing, no welcome email fires.
    /// </summary>
    [Test]
    public void Create_ShouldRaiseUserRegisteredEvent()
    {
        var user = User.Create("a@b.com", "hash", "John", "Doe");

        // DomainEvents collection must contain exactly one UserRegisteredEvent
        user.DomainEvents.Should().ContainSingle(e => e is UserRegisteredEvent);
    }

    /// <summary>
    /// A newly registered user must NOT have a verified email.
    /// They must go through the OTP verification flow first.
    /// </summary>
    [Test]
    public void Create_ShouldNotBeEmailVerified()
    {
        var user = User.Create("a@b.com", "hash", "John", "Doe");

        user.IsEmailVerified.Should().BeFalse();
    }

    // ── OTP ───────────────────────────────────────────────────────────────────
    // Tests for GenerateOtp() and ValidateOtp().
    // OTPs are used for two purposes: "email-verification" and "password-reset".
    // They are bcrypt-hashed before storage and expire after 10 minutes.

    /// <summary>
    /// The OTP returned by GenerateOtp() must be exactly 6 numeric digits.
    /// This matches the frontend input field (maxlength="6", type="text").
    /// </summary>
    [Test]
    public void GenerateOtp_ShouldReturn6DigitString()
    {
        var user = User.Create("a@b.com", "hash", "John", "Doe");

        // Act — generate an OTP for email verification
        var otp = user.GenerateOtp("email-verification");

        // Assert — must be exactly 6 digits
        otp.Should().HaveLength(6);
        otp.Should().MatchRegex(@"^\d{6}$"); // only digits, no letters
    }

    /// <summary>
    /// When the correct OTP is provided with the matching purpose,
    /// ValidateOtp() must return true.
    /// </summary>
    [Test]
    public void ValidateOtp_WithCorrectOtp_ShouldReturnTrue()
    {
        var user = User.Create("a@b.com", "hash", "John", "Doe");
        var otp = user.GenerateOtp("email-verification");

        // The same OTP that was generated must validate successfully
        user.ValidateOtp(otp, "email-verification").Should().BeTrue();
    }

    /// <summary>
    /// An incorrect OTP (e.g. "000000") must be rejected.
    /// BCrypt.Verify() will return false for a non-matching hash.
    /// </summary>
    [Test]
    public void ValidateOtp_WithWrongOtp_ShouldReturnFalse()
    {
        var user = User.Create("a@b.com", "hash", "John", "Doe");
        user.GenerateOtp("email-verification"); // generates and stores hash

        // "000000" is almost certainly not the generated OTP
        user.ValidateOtp("000000", "email-verification").Should().BeFalse();
    }

    /// <summary>
    /// An OTP generated for "email-verification" must NOT work for "password-reset".
    /// Purpose mismatch is checked before the hash comparison.
    /// This prevents an email-verification OTP from being reused to reset a password.
    /// </summary>
    [Test]
    public void ValidateOtp_WithWrongPurpose_ShouldReturnFalse()
    {
        var user = User.Create("a@b.com", "hash", "John", "Doe");
        var otp = user.GenerateOtp("email-verification");

        // Correct OTP but wrong purpose — must fail
        user.ValidateOtp(otp, "password-reset").Should().BeFalse();
    }

    /// <summary>
    /// After a successful OTP validation, OtpHash, OtpExpiry, and OtpPurpose
    /// must all be cleared (set to null). This is the "consume" step —
    /// once used, the OTP is gone from the database.
    /// </summary>
    [Test]
    public void ValidateOtp_ShouldClearOtpFieldsAfterSuccess()
    {
        var user = User.Create("a@b.com", "hash", "John", "Doe");
        var otp = user.GenerateOtp("email-verification");

        // Validate successfully — this should consume the OTP
        user.ValidateOtp(otp, "email-verification");

        // All OTP fields must be null after consumption
        user.OtpHash.Should().BeNull();
        user.OtpExpiry.Should().BeNull();
        user.OtpPurpose.Should().BeNull();
    }

    /// <summary>
    /// An OTP can only be used once (one-time password).
    /// After the first successful validation clears the fields,
    /// the second call with the same OTP must return false.
    /// This prevents replay attacks.
    /// </summary>
    [Test]
    public void ValidateOtp_UsedTwice_ShouldFailSecondTime()
    {
        var user = User.Create("a@b.com", "hash", "John", "Doe");
        var otp = user.GenerateOtp("email-verification");

        // First use — succeeds and clears the OTP
        user.ValidateOtp(otp, "email-verification").Should().BeTrue();

        // Second use — OtpHash is now null, so this must fail
        user.ValidateOtp(otp, "email-verification").Should().BeFalse();
    }

    // ── VerifyEmail ───────────────────────────────────────────────────────────

    /// <summary>
    /// Calling VerifyEmail() must flip IsEmailVerified to true.
    /// This is called by VerifyEmailHandler after a successful OTP check.
    /// Without this, the user cannot log in (login checks IsEmailVerified).
    /// </summary>
    [Test]
    public void VerifyEmail_ShouldSetIsEmailVerifiedTrue()
    {
        var user = User.Create("a@b.com", "hash", "John", "Doe");

        user.VerifyEmail();

        user.IsEmailVerified.Should().BeTrue();
    }

    // ── Deactivate ────────────────────────────────────────────────────────────

    /// <summary>
    /// Calling Deactivate() must set IsActive to false.
    /// The LoginHandler checks IsActive and returns "Account is deactivated"
    /// if this is false, preventing deactivated users from logging in.
    /// </summary>
    [Test]
    public void Deactivate_ShouldSetIsActiveFalse()
    {
        var user = User.Create("a@b.com", "hash", "John", "Doe");

        user.Deactivate();

        user.IsActive.Should().BeFalse();
    }

    // ── FullName ──────────────────────────────────────────────────────────────

    /// <summary>
    /// FullName is a computed property that concatenates FirstName and LastName
    /// with a space. Used in email templates (e.g. "Hi John Doe, ...").
    /// </summary>
    [Test]
    public void FullName_ShouldCombineFirstAndLastName()
    {
        var user = User.Create("a@b.com", "hash", "John", "Doe");

        user.FullName.Should().Be("John Doe");
    }
}
