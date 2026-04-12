using AuthService.Domain;
using SharedKernel.CQRS;

namespace AuthService.Application.Commands;

/// <summary>
/// Query to retrieve the authenticated user's profile.
/// Used by the <c>GET /api/v1/auth/me</c> endpoint to populate the frontend
/// user context on app load and after profile updates.
/// </summary>
public record GetUserProfileQuery(Guid UserId) : IQuery<UserProfileResponse>;

/// <summary>
/// Read model DTO representing the authenticated user's profile.
/// Excludes sensitive fields (password hash, refresh token, OTP data).
/// <c>IsEmailVerified</c> is included so the frontend can prompt the user
/// to verify their email if they registered with email/password.
/// </summary>
public record UserProfileResponse(Guid Id, string Email, string FirstName, string LastName,
    string Role, string? PhoneNumber, bool IsEmailVerified);

/// <summary>
/// Command to revoke a refresh token, effectively logging the user out.
/// The token is looked up by value and nulled out in the database so it
/// cannot be used to obtain new access tokens.
/// </summary>
public record RevokeTokenCommand(string RefreshToken) : ICommand;

/// <summary>
/// Handles <see cref="GetUserProfileQuery"/> by loading the user from the
/// repository and projecting it to a <see cref="UserProfileResponse"/>.
/// Throws <see cref="KeyNotFoundException"/> if the user does not exist —
/// this should not happen in practice since the user ID comes from a valid JWT,
/// but handles the edge case of a deleted account with a still-valid token.
/// </summary>
public class GetUserProfileHandler(IUserRepository repo)
    : IQueryHandler<GetUserProfileQuery, UserProfileResponse>
{
    /// <summary>
    /// Loads the user by ID and maps them to the profile response DTO.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Thrown if no user exists with the given ID.</exception>
    public async Task<UserProfileResponse> Handle(GetUserProfileQuery query, CancellationToken ct)
    {
        var user = await repo.GetByIdAsync(query.UserId, ct)
            ?? throw new KeyNotFoundException("User not found.");
        return new UserProfileResponse(user.Id, user.Email, user.FirstName, user.LastName,
            user.Role.ToString(), user.PhoneNumber, user.IsEmailVerified);
    }
}

/// <summary>
/// Handles <see cref="RevokeTokenCommand"/> by finding the user associated with
/// the refresh token and clearing it. If the token is not found (already revoked
/// or never issued), returns a failure result rather than throwing, since this
/// is a safe no-op from a security perspective.
/// </summary>
public class RevokeTokenHandler(IUserRepository repo) : ICommandHandler<RevokeTokenCommand>
{
    /// <summary>
    /// Looks up the user by refresh token, revokes it, and persists the change.
    /// </summary>
    public async Task<SharedKernel.Domain.Result> Handle(RevokeTokenCommand cmd, CancellationToken ct)
    {
        var user = await repo.GetByRefreshTokenAsync(cmd.RefreshToken, ct);
        if (user is null) return SharedKernel.Domain.Result.Failure("Token not found.");
        user.RevokeRefreshToken();
        await repo.UpdateAsync(user, ct);
        return SharedKernel.Domain.Result.Success();
    }
}
