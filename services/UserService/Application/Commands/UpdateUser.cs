using Microsoft.EntityFrameworkCore;
using UserService.Infrastructure;

namespace UserService.Application.Commands;

public record UpdateUserCommand(Guid Id, string? Email, string? FirstName, string? LastName, string? PhoneNumber);
public record ChangeRoleCommand(Guid Id, string Role);
public record ToggleActiveCommand(Guid Id);
public record DeleteUserCommand(Guid Id);

public class UpdateUserHandler(UserDbContext db)
{
    private static readonly string[] ValidRoles = ["Admin", "StoreManager", "DeliveryDriver", "Customer"];

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
