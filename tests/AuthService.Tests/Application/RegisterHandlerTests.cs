using AuthService.Application.Commands;
using AuthService.Application.Services;
using AuthService.Domain;
using AuthService.Infrastructure;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace AuthService.Tests.Application;

/// <summary>
/// Unit tests for <see cref="RegisterUserHandler"/>.
///
/// PURPOSE:
///   Validates all user registration scenarios — new email success,
///   duplicate email rejection, repository side-effects, and email casing.
///
/// TOOLS USED:
///   - NUnit            : test framework ([TestFixture], [Test], [SetUp])
///   - NSubstitute      : mocks IUserRepository, IPasswordHasher, INotificationRelay
///   - FluentAssertions : readable assertions (.Should().Be(), .Received())
///
/// MOCKING STRATEGY:
///   RegisterUserHandler depends on three interfaces:
///     - IUserRepository      : fake DB — ExistsAsync() and AddAsync()
///     - IPasswordHasher      : fake BCrypt — Hash() returns a dummy hash string
///     - INotificationRelay   : fake RabbitMQ publisher — fire-and-forget welcome email
///
///   INotificationRelay must be an interface (not a concrete class) so NSubstitute
///   can create a proxy. This is why we extracted INotificationRelay from
///   NotificationRelay — concrete classes with required constructors cannot be mocked.
///
/// WHAT IS NOT TESTED HERE:
///   - FluentValidation rules (RegisterUserValidator) — tested separately
///   - Password hashing algorithm — IPasswordHasher is mocked
///   - RabbitMQ publishing — INotificationRelay is mocked (fire-and-forget)
///   - Database persistence — IUserRepository is mocked
/// </summary>
[TestFixture]
public class RegisterHandlerTests
{
    // ── Dependencies (mocked) ─────────────────────────────────────────────────
    private IUserRepository _repo = null!;       // fake database
    private IPasswordHasher _hasher = null!;     // fake BCrypt
    private INotificationRelay _relay = null!;   // fake RabbitMQ publisher
    private RegisterUserHandler _handler = null!; // real class under test

    /// <summary>
    /// Runs before every [Test]. Creates fresh mocks so each test is fully
    /// isolated — no shared state between tests.
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        _repo    = Substitute.For<IUserRepository>();
        _hasher  = Substitute.For<IPasswordHasher>();
        _relay   = Substitute.For<INotificationRelay>();
        // Inject all mocks into the real handler
        _handler = new RegisterUserHandler(_repo, _hasher, _relay);
    }

    /// <summary>
    /// HAPPY PATH: registering with a brand-new email must succeed.
    ///
    /// Arrange: repo.ExistsAsync returns false (email not taken),
    ///          hasher.Hash returns a dummy hash string.
    /// Act:     call Handle() with valid registration data.
    /// Assert:  result is success, email and role are correct in the response.
    /// </summary>
    [Test]
    public async Task Handle_NewEmail_ReturnsSuccess()
    {
        // Arrange — email does not exist yet
        _repo.ExistsAsync("new@example.com", default).Returns(false);
        _hasher.Hash("Password1").Returns("hashed");

        var cmd = new RegisterUserCommand("new@example.com", "Password1", "Jane", "Doe", null);

        // Act
        var result = await _handler.Handle(cmd, default);

        // Assert — registration succeeded with correct response data
        result.IsSuccess.Should().BeTrue();
        result.Value!.Email.Should().Be("new@example.com");
        result.Value.Role.Should().Be("Customer"); // default role
    }

    /// <summary>
    /// BUSINESS RULE: registering with an already-taken email must fail.
    /// The error message is "Email already registered." — shown to the user
    /// on the registration form.
    ///
    /// Arrange: repo.ExistsAsync returns true (email already in DB).
    /// Assert:  failure with "Email already registered."
    ///          AddAsync must NOT be called (no partial user created).
    /// </summary>
    [Test]
    public async Task Handle_DuplicateEmail_ReturnsFailure()
    {
        // Arrange — email already exists in the database
        _repo.ExistsAsync("existing@example.com", default).Returns(true);

        var cmd = new RegisterUserCommand("existing@example.com", "Password1", "Jane", "Doe", null);

        // Act
        var result = await _handler.Handle(cmd, default);

        // Assert — registration rejected with correct error
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Email already registered.");
    }

    /// <summary>
    /// SIDE EFFECT: on successful registration, the handler must call
    /// repo.AddAsync() exactly once with the correct User entity.
    ///
    /// Uses NSubstitute's Arg.Is() to verify the User passed to AddAsync
    /// has the expected email — confirming the entity was built correctly.
    /// Uses .Received(1) to assert AddAsync was called exactly once.
    /// </summary>
    [Test]
    public async Task Handle_Success_AddsUserToRepository()
    {
        // Arrange
        _repo.ExistsAsync(Arg.Any<string>(), default).Returns(false);
        _hasher.Hash(Arg.Any<string>()).Returns("hashed");

        var cmd = new RegisterUserCommand("new@example.com", "Password1", "Jane", "Doe", null);

        // Act
        await _handler.Handle(cmd, default);

        // Assert — AddAsync was called once with a User whose email matches
        await _repo.Received(1).AddAsync(
            Arg.Is<User>(u => u.Email == "new@example.com"), default);
    }

    /// <summary>
    /// DATA INTEGRITY: the email in the response must be lowercase,
    /// even if the user typed it in mixed case ("NEW@EXAMPLE.COM").
    ///
    /// This is enforced by User.Create() which calls .ToLowerInvariant().
    /// The response DTO must reflect the normalized stored value.
    /// </summary>
    [Test]
    public async Task Handle_Success_EmailIsLowercased()
    {
        // Arrange — uppercase email input
        _repo.ExistsAsync(Arg.Any<string>(), default).Returns(false);
        _hasher.Hash(Arg.Any<string>()).Returns("hashed");

        var cmd = new RegisterUserCommand("NEW@EXAMPLE.COM", "Password1", "Jane", "Doe", null);

        // Act
        var result = await _handler.Handle(cmd, default);

        // Assert — response email is lowercase regardless of input casing
        result.Value!.Email.Should().Be("new@example.com");
    }
}
