using Microsoft.EntityFrameworkCore;
using UserService.Infrastructure;

namespace UserService.Application.Commands;

/// <summary>
/// Command to update a user's profile fields. Null or empty values for string
/// fields are treated as "no change" — only non-empty values overwrite the
/// existing data, preventing accidental data loss from partial updates.
/// </summary>
public record UpdateUserCommand(Guid Id, string? Email, string? FirstName, string? LastName, string? PhoneNumber);

/// <summary>
/// Command to change a user's role. The role must be one of the four defined
/// system roles: Admin, StoreManager, DeliveryDriver, Customer.
/// </summary>
public record ChangeRoleCommand(Guid Id, string Role);

/// <summary>
/// Command to toggle a user's active status. No payload needed — the handler
/// reads the current state and flips it.
/// </summary>
public record ToggleActiveCommand(Guid Id);

/// <summary>
/// Command to permanently delete a user account.
/// The handler revokes the refresh token before deletion to invalidate sessions.
/// </summary>
public record DeleteUserCommand(Guid Id);

/// <summary>
/// Application service handler for all user write operations in the admin context.
/// Operates directly on the <see cref="UserDbContext"/> (which maps to the same
/// <c>Users</c> table as AuthService) to keep user data consistent across services.
/// </summary>
public class UpdateUserHandler(UserDbContext db)
{
    /// <summary>
    /// The four valid system roles. Validated here rather than relying solely on
    /// a database constraint so we can return a descriptive error message.
    /// </summary>
    private static readonly string[] ValidRoles = ["Admin", "StoreManager", "DeliveryDriver", "Customer"];

    /// <summary>
    /// Updates a user's profile fields. Only non-null, non-empty values are applied.
    /// Email uniqueness is checked before updating to prevent duplicate accounts.
    /// </summary>
    /// <returns>
    /// (true, null, userDto) on success.
    /// (false, "User not found", null) if the user does not exist.
    /// (false, "Email already in use", null) if the new email conflicts with another account.
    /// </returns>
    public async Task<(bool Success, string? Error, object? Result)> UpdateAsync(
        UpdateUserCommand cmd, CancellationToken ct = default)
    {
        var u = await db.Users.FindAsync([cmd.Id], ct);
        if (u is null) return (false, "User not found", null);

        if (!string.IsNullOrWhiteSpace(cmd.Email) && cmd.Email != u.Email)
        {
            if (await db.Users.AnyAsync(x => x.Email == cmd.Email.ToLower() && x.Id != cmd.Id, ct))
                return (false, "Email already in use", null);
            u.Email = cmd.Email.ToLower();
        }
        if (!string.IsNullOrWhiteSpace(cmd.FirstName)) u.FirstName = cmd.FirstName;
        if (!string.IsNullOrWhiteSpace(cmd.LastName)) u.LastName = cmd.LastName;
        u.PhoneNumber = cmd.PhoneNumber;
        await db.SaveChangesAsync(ct);

        return (true, null, new { id = u.Id.ToString(), u.Email, u.FirstName, u.LastName, u.Role, u.PhoneNumber, u.IsActive });
    }

    /// <summary>
    /// Changes a user's role after validating that the requested role is one of
    /// the four defined system roles. Role changes take effect on the user's next
    /// login since existing JWTs are not invalidated.
    /// </summary>
    /// <returns>
    /// (true, null, {id, role}) on success.
    /// (false, "Invalid role", null) if the role string is not recognised.
    /// (false, "User not found", null) if the user does not exist.
    /// </returns>
    public async Task<(bool Success, string? Error, object? Result)> ChangeRoleAsync(
        ChangeRoleCommand cmd, CancellationToken ct = default)
    {
        if (!ValidRoles.Contains(cmd.Role)) return (false, "Invalid role", null);
        var u = await db.Users.FindAsync([cmd.Id], ct);
        if (u is null) return (false, "User not found", null);
        u.Role = cmd.Role;
        await db.SaveChangesAsync(ct);
        return (true, null, new { id = u.Id, u.Role });
    }

    /// <summary>
    /// Toggles the user's <c>IsActive</c> flag. When deactivating, the refresh
    /// token is immediately nulled out so the user cannot silently renew their
    /// access token — they will be forced to log in again, at which point the
    /// inactive check will block them.
    /// </summary>
    /// <returns>(true, {id, isActive}) on success, (false, null) if the user is not found.</returns>
    public async Task<(bool Success, object? Result)> ToggleActiveAsync(
        ToggleActiveCommand cmd, CancellationToken ct = default)
    {
        var u = await db.Users.FindAsync([cmd.Id], ct);
        if (u is null) return (false, null);
        u.IsActive = !u.IsActive;

        // Revoke refresh token immediately when deactivating
        // so the user cannot silently get a new access token
        if (!u.IsActive)
        {
            u.RefreshToken = null;
            u.RefreshTokenExpiry = null;
        }

        await db.SaveChangesAsync(ct);
        return (true, new { id = u.Id, u.IsActive });
    }

    /// <summary>
    /// Permanently deletes a user account. The refresh token is revoked in a
    /// separate save before the entity is removed, ensuring that any concurrent
    /// token refresh attempts fail immediately rather than succeeding against a
    /// now-deleted account.
    /// </summary>
    /// <returns><c>true</c> if the user was found and deleted; <c>false</c> if not found.</returns>
    public async Task<bool> DeleteAsync(DeleteUserCommand cmd, CancellationToken ct = default)
    {
        var u = await db.Users.FindAsync([cmd.Id], ct);
        if (u is null) return false;

        // Revoke refresh token before deletion so any in-flight
        // token refresh attempts fail immediately
        u.RefreshToken = null;
        u.RefreshTokenExpiry = null;
        await db.SaveChangesAsync(ct);

        db.Users.Remove(u);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
