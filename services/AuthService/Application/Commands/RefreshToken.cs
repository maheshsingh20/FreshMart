using AuthService.Domain;
using AuthService.Application.Services;
using SharedKernel.CQRS;
using SharedKernel.Domain;

namespace AuthService.Application.Commands;

public record RefreshTokenCommand(string RefreshToken) : ICommand<AuthTokenResponse>;

public class RefreshTokenHandler(IUserRepository repo, IJwtService jwt)
    : ICommandHandler<RefreshTokenCommand, AuthTokenResponse>
{
    public async Task<Result<AuthTokenResponse>> Handle(RefreshTokenCommand cmd, CancellationToken ct)
    {
        var user = await repo.GetByRefreshTokenAsync(cmd.RefreshToken, ct);
        if (user is null || user.RefreshTokenExpiry < DateTime.UtcNow)
            return Result<AuthTokenResponse>.Failure("Invalid or expired refresh token.");

        // Block deactivated users from getting new tokens
        if (!user.IsActive)
            return Result<AuthTokenResponse>.Failure("Account is deactivated.");

        var accessToken = jwt.GenerateAccessToken(user);
        var (newRefresh, expiry) = jwt.GenerateRefreshToken();

        user.SetRefreshToken(newRefresh, expiry);
        await repo.UpdateAsync(user, ct);

        return Result<AuthTokenResponse>.Success(
            new AuthTokenResponse(accessToken, newRefresh, expiry, user.Role.ToString(), user.Id));
    }
}
