namespace AuthService.Domain;

/// <summary>
/// Repository contract for <see cref="User"/> aggregate persistence.
/// Abstracts EF Core so application handlers remain infrastructure-agnostic and testable.
/// </summary>
public interface IUserRepository
{
    /// <summary>Retrieves a user by their unique identifier.</summary>
    /// <param name="id">The user's GUID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching user, or <c>null</c> if not found.</returns>
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a user by their normalised email address.
    /// Used during login and duplicate-email checks.
    /// </summary>
    /// <param name="email">Raw email — normalised to lowercase before querying.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a user by their active refresh token.
    /// Used during token refresh to validate the token and rotate it.
    /// </summary>
    /// <param name="token">The opaque refresh token string.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<User?> GetByRefreshTokenAsync(string token, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a user by their Google OAuth subject identifier.
    /// Used during Google sign-in to find an existing linked account.
    /// </summary>
    /// <param name="googleId">The "sub" claim from the Google ID token.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<User?> GetByGoogleIdAsync(string googleId, CancellationToken ct = default);

    /// <summary>
    /// Checks whether an account with the given email already exists.
    /// Used during registration to prevent duplicate accounts.
    /// </summary>
    /// <param name="email">Raw email — normalised to lowercase before querying.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>true</c> if an account with this email exists.</returns>
    Task<bool> ExistsAsync(string email, CancellationToken ct = default);

    /// <summary>Persists a newly created user and saves changes.</summary>
    /// <param name="user">The user aggregate to add.</param>
    /// <param name="ct">Cancellation token.</param>
    Task AddAsync(User user, CancellationToken ct = default);

    /// <summary>Persists changes to an existing user and saves changes.</summary>
    /// <param name="user">The modified user aggregate.</param>
    /// <param name="ct">Cancellation token.</param>
    Task UpdateAsync(User user, CancellationToken ct = default);
}
