using AuthService.Application.Commands;
using AuthService.Application.Services;
using AuthService.Domain;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace AuthService.Tests.Application;

/// <summary>
/// Unit tests for <see cref="LoginHandler"/>.
///
/// PURPOSE:
///   Validates all login scenarios — valid credentials, wrong password,
///   user not found, deactivated account, and side-effects (refresh token saved).
///
/// TOOLS USED:
///   - NUnit            : test framework
///   - NSubstitute      : mocks IUserRepository, IPasswordHasher, IJwtService
///   - FluentAssertions : readable assertions
///
/// MOCKING STRATEGY:
///   LoginHandler depends on three interfaces:
///     - IUserRepository  : fake DB — returns a User or null
///     - IPasswordHasher  : fake BCrypt — returns true/false for Verify()
///     - IJwtService      : fake JWT — returns dummy token strings
///
///   NSubstitute creates in-memory fakes via Substitute.For&lt;T&gt;().
///   We configure return values with .Returns() and verify calls with .Received().
///
/// WHAT IS NOT TESTED HERE:
///   - FluentValidation rules (LoginValidator) — those are tested separately
///   - JWT token format/expiry — IJwtService is mocked, not the real implementation
///   - Database queries — IUserRepository is mocked
/// </summary>
[TestFixture]
public class LoginHandlerTests
{
    // ── Dependencies (mocked) ─────────────────────────────────────────────────
    private IUserRepository _repo = null!;   // fake database
    private IPasswordHasher _hasher = null!; // fake BCrypt
    private IJwtService _jwt = null!;        // fake JWT generator
    private LoginHandler _handler = null!;   // the real class under test

    /// <summary>
    /// Runs before every [Test]. Creates fresh mocks so tests are fully isolated —
    /// no state leaks between tests.
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        _repo    = Substitute.For<IUserRepository>();
        _hasher  = Substitute.For<IPasswordHasher>();
        _jwt     = Substitute.For<IJwtService>();
        // Inject the mocks into the real handler
        _handler = new LoginHandler(_repo, _hasher, _jwt);
    }

    /// <summary>
    /// HAPPY PATH: correct email + correct password for an active user.
    /// Expected: Result.IsSuccess = true, AccessToken and Role are populated.
    ///
    /// Arrange: repo returns a valid user, hasher confirms password, jwt returns tokens.
    /// Act:     call Handle() with matching credentials.
    /// Assert:  result is success, access token matches, role is "Customer".
    /// </summary>
    [Test]
    public async Task Handle_ValidCredentials_ReturnsSuccess()
    {
        // Arrange
        var user = User.Create("john@example.com", "hashed", "John", "Doe");
        _repo.GetByEmailAsync("john@example.com", default).Returns(user);
        _hasher.Verify("Password1", "hashed").Returns(true);  // password matches
        _jwt.GenerateAccessToken(user).Returns("access-token");
        _jwt.GenerateRefreshToken().Returns(("refresh-token", DateTime.UtcNow.AddDays(7)));

        // Act
        var result = await _handler.Handle(
            new LoginCommand("john@example.com", "Password1"), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.AccessToken.Should().Be("access-token");
        result.Value.Role.Should().Be("Customer");
    }

    /// <summary>
    /// SECURITY: when the email doesn't exist in the database, the handler must
    /// return "Invalid credentials." — NOT "User not found." — to prevent
    /// email enumeration attacks (attacker can't tell if email is registered).
    ///
    /// Arrange: repo returns null for any email.
    /// Assert:  failure with generic "Invalid credentials." message.
    /// </summary>
    [Test]
    public async Task Handle_UserNotFound_ReturnsFailure()
    {
        // Arrange — repo returns null (user doesn't exist)
        _repo.GetByEmailAsync(Arg.Any<string>(), default).Returns((User?)null);

        // Act
        var result = await _handler.Handle(new LoginCommand("x@x.com", "pass"), default);

        // Assert — generic error, not "user not found" (prevents enumeration)
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Invalid credentials.");
    }

    /// <summary>
    /// SECURITY: when the email exists but the password is wrong, the same
    /// generic "Invalid credentials." message is returned (same as user not found).
    /// This prevents attackers from knowing whether the email is registered.
    ///
    /// Arrange: repo returns a user, but hasher.Verify() returns false.
    /// Assert:  failure with "Invalid credentials."
    /// </summary>
    [Test]
    public async Task Handle_WrongPassword_ReturnsFailure()
    {
        // Arrange — user exists but password doesn't match
        var user = User.Create("john@example.com", "hashed", "John", "Doe");
        _repo.GetByEmailAsync("john@example.com", default).Returns(user);
        _hasher.Verify("wrong", "hashed").Returns(false); // password mismatch

        // Act
        var result = await _handler.Handle(
            new LoginCommand("john@example.com", "wrong"), default);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Invalid credentials.");
    }

    /// <summary>
    /// BUSINESS RULE: a deactivated account must not be able to log in,
    /// even with correct credentials. The error message is different here
    /// ("Account is deactivated.") so admins can distinguish this case in logs.
    ///
    /// Arrange: user exists, password is correct, but user.IsActive = false.
    /// Assert:  failure with "Account is deactivated."
    /// </summary>
    [Test]
    public async Task Handle_DeactivatedUser_ReturnsFailure()
    {
        // Arrange — valid user but deactivated
        var user = User.Create("john@example.com", "hashed", "John", "Doe");
        user.Deactivate(); // sets IsActive = false
        _repo.GetByEmailAsync("john@example.com", default).Returns(user);
        _hasher.Verify("Password1", "hashed").Returns(true);

        // Act
        var result = await _handler.Handle(
            new LoginCommand("john@example.com", "Password1"), default);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Account is deactivated.");
    }

    /// <summary>
    /// SIDE EFFECT: on successful login, the handler must:
    ///   1. Set the refresh token on the User entity
    ///   2. Call repo.UpdateAsync() to persist it to the database
    ///
    /// This verifies the handler's write side-effect, not just the return value.
    /// Uses NSubstitute's .Received(1) to assert UpdateAsync was called exactly once.
    /// </summary>
    [Test]
    public async Task Handle_Success_SetsRefreshTokenOnUser()
    {
        // Arrange
        var user = User.Create("john@example.com", "hashed", "John", "Doe");
        _repo.GetByEmailAsync("john@example.com", default).Returns(user);
        _hasher.Verify("Password1", "hashed").Returns(true);
        _jwt.GenerateAccessToken(user).Returns("access-token");
        _jwt.GenerateRefreshToken().Returns(("refresh-token", DateTime.UtcNow.AddDays(7)));

        // Act
        await _handler.Handle(new LoginCommand("john@example.com", "Password1"), default);

        // Assert — refresh token was set on the entity
        user.RefreshToken.Should().Be("refresh-token");
        // Assert — UpdateAsync was called exactly once to persist the token
        await _repo.Received(1).UpdateAsync(user, default);
    }
}
