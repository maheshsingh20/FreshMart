using AuthService.Domain;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Persistence;

/// <summary>
/// Entity Framework Core implementation of <see cref="IUserRepository"/>.
/// Provides all data access operations for the <see cref="User"/> aggregate root.
/// Each method is a thin wrapper around EF Core queries, keeping the domain layer
/// free of persistence concerns. All lookups are case-insensitive for email
/// (normalised to lowercase) to prevent duplicate accounts from case variations.
/// </summary>
public class UserRepository(AuthDbContext db) : IUserRepository
{
    /// <summary>
    /// Retrieves a user by their unique identifier.
    /// Returns <c>null</c> if no user with the given ID exists.
    /// </summary>
    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    /// <summary>
    /// Retrieves a user by their email address (case-insensitive).
    /// Used during login and Google OAuth to find existing accounts.
    /// Returns <c>null</c> if no user with the given email exists.
    /// </summary>
    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        db.Users.FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), ct);

    /// <summary>
    /// Checks whether a user with the given email address already exists.
    /// Used during registration to enforce email uniqueness before attempting
    /// to insert, providing a descriptive error rather than a database exception.
    /// </summary>
    public Task<bool> ExistsAsync(string email, CancellationToken ct = default) =>
        db.Users.AnyAsync(u => u.Email == email.ToLowerInvariant(), ct);

    /// <summary>
    /// Retrieves a user by their non-expired refresh token.
    /// Used by the token refresh endpoint to validate the token and identify
    /// the user without requiring them to re-authenticate.
    /// Returns <c>null</c> if the token is not found or has expired.
    /// </summary>
    public Task<User?> GetByRefreshTokenAsync(string token, CancellationToken ct = default) =>
        db.Users.FirstOrDefaultAsync(u => u.RefreshToken == token && u.RefreshTokenExpiry > DateTime.UtcNow, ct);

    /// <summary>
    /// Retrieves a user by their Google account ID (<c>sub</c> claim from the Google token).
    /// Used during Google Sign-In to find returning users who previously authenticated
    /// with Google, avoiding a duplicate account lookup by email.
    /// Returns <c>null</c> if no user is linked to the given Google ID.
    /// </summary>
    public Task<User?> GetByGoogleIdAsync(string googleId, CancellationToken ct = default) =>
        db.Users.FirstOrDefaultAsync(u => u.GoogleId == googleId, ct);

    /// <summary>
    /// Persists a new user to the database.
    /// Saves immediately so the caller can rely on the user being persisted
    /// before returning from the registration or Google auth flow.
    /// </summary>
    public async Task AddAsync(User user, CancellationToken ct = default)
    {
        await db.Users.AddAsync(user, ct);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Persists changes to an existing user entity.
    /// Called after any mutation (profile update, token refresh, role change, etc.)
    /// to ensure the updated state is written to the database.
    /// </summary>
    public async Task UpdateAsync(User user, CancellationToken ct = default)
    {
        db.Users.Update(user);
        await db.SaveChangesAsync(ct);
    }
}
