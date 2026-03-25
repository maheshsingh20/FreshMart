using AuthService.Application.Services;
using AuthService.Domain;
using SharedKernel.CQRS;
using SharedKernel.Domain;
using System.Text.Json;

namespace AuthService.Application.Commands;

public record GoogleAuthCommand(string IdToken) : ICommand<AuthTokenResponse>;

public class GoogleAuthHandler(
    IUserRepository repo,
    IJwtService jwt,
    IHttpClientFactory http) : ICommandHandler<GoogleAuthCommand, AuthTokenResponse>
{
    public async Task<Result<AuthTokenResponse>> Handle(GoogleAuthCommand cmd, CancellationToken ct)
    {
        // Verify token with Google
        var client = http.CreateClient();
        var resp = await client.GetAsync(
            $"https://oauth2.googleapis.com/tokeninfo?id_token={cmd.IdToken}", ct);

        if (!resp.IsSuccessStatusCode)
            return Result<AuthTokenResponse>.Failure("Invalid Google token.");

        var json = await resp.Content.ReadAsStringAsync(ct);
        var payload = JsonDocument.Parse(json).RootElement;

        var googleId  = payload.GetProperty("sub").GetString()!;
        var email     = payload.GetProperty("email").GetString()!.ToLowerInvariant();
        var firstName = payload.TryGetProperty("given_name",  out var gn) ? gn.GetString() ?? "" : "";
        var lastName  = payload.TryGetProperty("family_name", out var fn) ? fn.GetString() ?? "" : "";

        // Find by GoogleId first, then by email
        var user = await repo.GetByGoogleIdAsync(googleId, ct)
                   ?? await repo.GetByEmailAsync(email, ct);

        if (user is null)
        {
            user = User.CreateViaGoogle(email, firstName, lastName, googleId);
            await repo.AddAsync(user, ct);
        }
        else
        {
            if (user.GoogleId is null) user.LinkGoogle(googleId);
        }

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
