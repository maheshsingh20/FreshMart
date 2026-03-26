using AuthService.Application.Commands;
using AuthService.Application.Services;
using AuthService.Domain;
using AuthService.Infrastructure;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace AuthService.Tests.Application;

/// <summary>
/// Unit tests for the three OTP-related command handlers:
///   - <see cref="SendOtpHandler"/>     : generates and stores an OTP, publishes via RabbitMQ
///   - <see cref="VerifyEmailHandler"/> : validates OTP and marks email as verified
///   - <see cref="ResetPasswordHandler"/>: validates OTP and replaces the password hash
///
/// PURPOSE:
///   Validates the full OTP lifecycle — generation, delivery, verification,
///   and password reset — including security edge cases like email enumeration
///   prevention and one-time-use enforcement.
///
/// TOOLS USED:
///   - NUnit            : test framework
///   - NSubstitute      : mocks IUserRepository, INotificationRelay, IPasswordHasher
///   - FluentAssertions : readable assertions
///
/// MOCKING STRATEGY:
///   Each handler is constructed fresh per test (not in [SetUp]) because
///   different tests need different handler combinations:
///     - SendOtpHandler     needs _repo + _relay
///     - VerifyEmailHandler needs _repo only
///     - ResetPasswordHandler needs _repo + a local hasher mock
///
///   _repo and _relay are shared mocks reset in [SetUp].
///   Local hasher mocks are created inline where needed.
///
/// SECURITY TESTS INCLUDED:
///   - Email enumeration prevention (SendOtp returns success even for unknown emails)
///   - OTP one-time-use (tested in UserTests — domain layer)
///   - Wrong OTP rejection
///   - Wrong purpose rejection (tested in UserTests — domain layer)
/// </summary>
[TestFixture]
public class OtpHandlerTests
{
    // ── Shared mocks ──────────────────────────────────────────────────────────
    private IUserRepository _repo = null!;     // fake database
    private INotificationRelay _relay = null!; // fake RabbitMQ publisher

    /// <summary>
    /// Runs before every [Test]. Creates fresh mocks to ensure test isolation.
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        _repo  = Substitute.For<IUserRepository>();
        _relay = Substitute.For<INotificationRelay>();
    }

    // ── SendOtp ───────────────────────────────────────────────────────────────
    // SendOtpHandler: looks up the user, calls user.GenerateOtp(), saves to DB,
    // then fires relay.NotifyOtpAsync() (fire-and-forget via RabbitMQ).

    /// <summary>
    /// HAPPY PATH: when the user exists, SendOtp must return success.
    ///
    /// Arrange: repo returns a valid user.
    /// Assert:  result is success (OTP was generated and queued for delivery).
    /// </summary>
    [Test]
    public async Task SendOtp_UserExists_ReturnsSuccess()
    {
        // Arrange — user exists in the database
        var user = User.Create("a@b.com", "hash", "John", "Doe");
        _repo.GetByEmailAsync("a@b.com", default).Returns(user);

        var handler = new SendOtpHandler(_repo, _relay);

        // Act
        var result = await handler.Handle(
            new SendOtpCommand("a@b.com", "email-verification"), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    /// <summary>
    /// SECURITY — EMAIL ENUMERATION PREVENTION:
    /// When the email does NOT exist, SendOtp must still return success.
    ///
    /// Why? If we returned an error for unknown emails, an attacker could
    /// probe the system to discover which emails are registered.
    /// By always returning success, we reveal nothing about account existence.
    ///
    /// Arrange: repo returns null (user not found).
    /// Assert:  result is STILL success — no error, no information leak.
    /// </summary>
    [Test]
    public async Task SendOtp_UserNotFound_StillReturnsSuccess_ToPreventEnumeration()
    {
        // Arrange — email does not exist in the database
        _repo.GetByEmailAsync(Arg.Any<string>(), default).Returns((User?)null);

        var handler = new SendOtpHandler(_repo, _relay);

        // Act
        var result = await handler.Handle(
            new SendOtpCommand("ghost@b.com", "email-verification"), default);

        // Assert — success even for non-existent email (security requirement)
        result.IsSuccess.Should().BeTrue();
    }

    /// <summary>
    /// SIDE EFFECT: when the user exists, the handler must:
    ///   1. Call user.GenerateOtp() — which sets OtpHash on the entity
    ///   2. Call repo.UpdateAsync() — to persist the OTP hash to the database
    ///
    /// Verifies both the entity state change and the repository write.
    /// </summary>
    [Test]
    public async Task SendOtp_UserExists_UpdatesUserWithOtp()
    {
        // Arrange
        var user = User.Create("a@b.com", "hash", "John", "Doe");
        _repo.GetByEmailAsync("a@b.com", default).Returns(user);

        var handler = new SendOtpHandler(_repo, _relay);

        // Act
        await handler.Handle(new SendOtpCommand("a@b.com", "email-verification"), default);

        // Assert — OTP was stored on the entity (GenerateOtp was called)
        user.OtpHash.Should().NotBeNull();
        // Assert — UpdateAsync was called to persist the OTP hash
        await _repo.Received(1).UpdateAsync(user, default);
    }

    // ── VerifyEmail ───────────────────────────────────────────────────────────
    // VerifyEmailHandler: looks up user, calls user.ValidateOtp(), then
    // calls user.VerifyEmail() to set IsEmailVerified = true.

    /// <summary>
    /// HAPPY PATH: correct OTP for the correct purpose must succeed and
    /// mark the user's email as verified.
    ///
    /// Arrange: generate a real OTP on the user entity, then pass it to the handler.
    /// Assert:  result is success AND user.IsEmailVerified is now true.
    /// </summary>
    [Test]
    public async Task VerifyEmail_ValidOtp_ReturnsSuccessAndVerifiesEmail()
    {
        // Arrange — generate a real OTP directly on the domain entity
        var user = User.Create("a@b.com", "hash", "John", "Doe");
        var otp = user.GenerateOtp("email-verification"); // returns the plain OTP
        _repo.GetByEmailAsync("a@b.com", default).Returns(user);

        var handler = new VerifyEmailHandler(_repo);

        // Act — submit the correct OTP
        var result = await handler.Handle(new VerifyEmailCommand("a@b.com", otp), default);

        // Assert — success and email is now verified
        result.IsSuccess.Should().BeTrue();
        user.IsEmailVerified.Should().BeTrue();
    }

    /// <summary>
    /// VALIDATION: submitting the wrong OTP must fail with a clear error message.
    /// "000000" is used as a deliberately wrong value.
    ///
    /// Arrange: generate a real OTP (so OtpHash is set), but submit "000000".
    /// Assert:  failure with "Invalid or expired OTP."
    /// </summary>
    [Test]
    public async Task VerifyEmail_WrongOtp_ReturnsFailure()
    {
        // Arrange — OTP is generated but we submit the wrong value
        var user = User.Create("a@b.com", "hash", "John", "Doe");
        user.GenerateOtp("email-verification"); // sets OtpHash internally
        _repo.GetByEmailAsync("a@b.com", default).Returns(user);

        var handler = new VerifyEmailHandler(_repo);

        // Act — submit wrong OTP
        var result = await handler.Handle(new VerifyEmailCommand("a@b.com", "000000"), default);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Invalid or expired OTP.");
    }

    /// <summary>
    /// EDGE CASE: if the email doesn't exist in the database, return failure.
    /// Unlike SendOtp, VerifyEmail CAN reveal that the email is unknown
    /// because the user already knows their own email.
    /// </summary>
    [Test]
    public async Task VerifyEmail_UserNotFound_ReturnsFailure()
    {
        // Arrange — user does not exist
        _repo.GetByEmailAsync(Arg.Any<string>(), default).Returns((User?)null);

        var handler = new VerifyEmailHandler(_repo);

        // Act
        var result = await handler.Handle(
            new VerifyEmailCommand("ghost@b.com", "123456"), default);

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    // ── ResetPassword ─────────────────────────────────────────────────────────
    // ResetPasswordHandler: looks up user, validates OTP with purpose="password-reset",
    // then calls user.ResetPassword(hasher.Hash(newPassword)) to replace the hash.

    /// <summary>
    /// HAPPY PATH: correct OTP + valid new password must succeed and
    /// update the user's password hash in the entity.
    ///
    /// Arrange: generate a real OTP with purpose "password-reset",
    ///          configure a local hasher mock to return "newhash".
    /// Assert:  result is success AND user.PasswordHash is now "newhash".
    /// </summary>
    [Test]
    public async Task ResetPassword_ValidOtp_ChangesPasswordHash()
    {
        // Arrange — generate a real password-reset OTP
        var user = User.Create("a@b.com", "oldhash", "John", "Doe");
        var otp = user.GenerateOtp("password-reset");
        _repo.GetByEmailAsync("a@b.com", default).Returns(user);

        // Local hasher mock — returns "newhash" when hashing "NewPass1"
        var hasher = Substitute.For<IPasswordHasher>();
        hasher.Hash("NewPass1").Returns("newhash");

        var handler = new ResetPasswordHandler(_repo, hasher);

        // Act
        var result = await handler.Handle(
            new ResetPasswordCommand("a@b.com", otp, "NewPass1"), default);

        // Assert — success and password hash was replaced
        result.IsSuccess.Should().BeTrue();
        user.PasswordHash.Should().Be("newhash"); // old hash is gone
    }

    /// <summary>
    /// VALIDATION: submitting the wrong OTP for password reset must fail.
    /// The password must NOT be changed when OTP validation fails.
    ///
    /// Arrange: generate a real OTP but submit "000000" instead.
    /// Assert:  failure with "Invalid or expired OTP."
    ///          hasher.Hash() must NOT be called (no password change attempted).
    /// </summary>
    [Test]
    public async Task ResetPassword_WrongOtp_ReturnsFailure()
    {
        // Arrange — OTP is generated but we submit the wrong value
        var user = User.Create("a@b.com", "oldhash", "John", "Doe");
        user.GenerateOtp("password-reset");
        _repo.GetByEmailAsync("a@b.com", default).Returns(user);

        var hasher = Substitute.For<IPasswordHasher>();
        var handler = new ResetPasswordHandler(_repo, hasher);

        // Act — submit wrong OTP
        var result = await handler.Handle(
            new ResetPasswordCommand("a@b.com", "000000", "NewPass1"), default);

        // Assert — failure, password unchanged
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Invalid or expired OTP.");
        // hasher.Hash() should never have been called
        hasher.DidNotReceive().Hash(Arg.Any<string>());
    }
}
