using AuthService.Domain;
using AuthService.Application.Services;
using FluentValidation;
using SharedKernel.CQRS;
using SharedKernel.Domain;

namespace AuthService.Application.Commands;

public record LoginCommand(string Email, string Password) : ICommand<AuthTokenResponse>;

public record AuthTokenResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    string Role,
    Guid UserId);

public class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

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
