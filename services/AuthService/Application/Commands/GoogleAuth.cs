using AuthService.Application.Services;
using AuthService.Domain;
using SharedKernel.CQRS;
using SharedKernel.Domain;
using System.Text.Json;

namespace AuthService.Application.Commands;

/// <summary>
/// Command to authenticate or register a user via Google Sign-In.
/// Carries the Google ID token issued by the Google Sign-In SDK on the frontend.
/// The token is verified server-side with Google's tokeninfo endpoint to prevent
/// token forgery — never trust a Google token without server-side verification.
/// </summary>
public record GoogleAuthCommand(string IdToken) : ICommand<AuthTokenResponse>;

/// <summary>
/// Handles <see cref="GoogleAuthCommand"/> by implementing the Google OAuth
/// server-side token verification and account linking/creation flow.
/// The flow is:
/// <list type="number">
///   <item>Verify the ID token with Google's tokeninfo API.</item>
///   <item>Extract the user's Google ID, email, and name from the token payload.</item>
///   <item>Look up an existing account by Google ID (returning user), then by email
///         (existing email/password user signing in with Google for the first time).</item>
///   <item>If no account exists, create a new one via Google (no password set).</item>
///   <item>If an existing email/password account is found, link the Google ID to it.</item>
///   <item>Issue a JWT access token and refresh token.</item>
/// </list>
/// This design allows users who registered with email/password to later sign in
/// with Google using the same email without creating a duplicate account.
/// </summary>
public class GoogleAuthHandler(
    IUserRepository repo,
    IJwtService jwt,
    IHttpClientFactory http) : ICommandHandler<GoogleAuthCommand, AuthTokenResponse>
{
    /// <summary>
    /// Verifies the Google ID token, resolves or creates the user account,
    /// and issues authentication tokens.
    /// </summary>
    /// <returns>
    /// <see cref="Result{T}.Success"/> with JWT tokens and role on success.
    /// <see cref="Result{T}.Failure"/> if the Google token is invalid or the account is deactivated.
    /// </returns>
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
