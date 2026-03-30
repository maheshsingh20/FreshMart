using AuthService.Domain;
using AuthService.Application.Services;
using FluentValidation;
using SharedKernel.CQRS;
using SharedKernel.Domain;

namespace AuthService.Application.Commands;

/// <summary>
/// Command to authenticate a user with email and password.
/// Returns JWT access token + refresh token on success.
/// </summary>
public record LoginCommand(string Email, string Password) : ICommand<AuthTokenResponse>;

/// <summary>
/// Response returned on successful login.
/// AccessToken: short-lived JWT (60 min) — sent in Authorization header for API calls.
/// RefreshToken: long-lived opaque token — used to get a new AccessToken without re-login.
/// Role: used by the frontend to redirect to the correct dashboard.
/// </summary>
public record AuthTokenResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    string Role,
    Guid UserId);

/// <summary>
/// FluentValidation rules for LoginCommand.
/// Runs automatically via MediatR pipeline before the handler executes.
/// Returns 400 Bad Request if validation fails — handler never runs.
/// </summary>
public class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

/// <summary>
/// Handles the login flow:
///   1. Look up user by email
///   2. Verify password hash (BCrypt)
///   3. Check account is active
///   4. Generate JWT access token + refresh token
///   5. Persist refresh token to DB
///   6. Return tokens to caller
///
/// SECURITY NOTE: Returns the same "Invalid credentials." error for both
/// "user not found" and "wrong password" to prevent email enumeration attacks.
/// </summary>
public class LoginHandler(IUserRepository repo, IPasswordHasher hasher, IJwtService jwt)
    : ICommandHandler<LoginCommand, AuthTokenResponse>
{
    public async Task<Result<AuthTokenResponse>> Handle(LoginCommand cmd, CancellationToken ct)
    {
        var user = await repo.GetByEmailAsync(cmd.Email, ct);
        if (user is null || !hasher.Verify(cmd.Password, user.PasswordHash))
            return Result<AuthTokenResponse>.Failure("Invalid credentials.");

        if (!user.IsActive)
            return Result<AuthTokenResponse>.Failure("Account is deactivated.");

        var accessToken = jwt.GenerateAccessToken(user);
        var (refreshToken, expiry) = jwt.GenerateRefreshToken();

        user.SetRefreshToken(refreshToken, expiry);
        await repo.UpdateAsync(user, ct);

        return Result<AuthTokenResponse>.Success(
            new AuthTokenResponse(accessToken, refreshToken, expiry, user.Role.ToString(), user.Id));
    }
}
