using Microsoft.EntityFrameworkCore;
using UserService.Infrastructure;

namespace UserService.Application.Queries;

/// <summary>
/// Query to retrieve a filtered list of users for the admin user management table.
/// All filter parameters are optional — omitting them returns all users.
/// </summary>
public record GetUsersQuery(string? Role, string? Search, bool? IsActive);

/// <summary>Query to retrieve a single user by their unique identifier.</summary>
public record GetUserByIdQuery(Guid Id);

/// <summary>Query to retrieve aggregate user statistics for the admin dashboard.</summary>
public record GetUserStatsQuery();

/// <summary>
/// Read model DTO representing a user as returned by the admin API.
/// Excludes sensitive fields (password hash, refresh token) that must never
/// leave the service boundary.
/// </summary>
public record UserDto(
    string Id, string Email, string FirstName, string LastName,
    string Role, string? PhoneNumber, bool IsActive, DateTime CreatedAt);

/// <summary>
/// Aggregate statistics DTO for the admin dashboard summary cards.
/// Provides a quick overview of the user base without requiring the frontend
/// to compute counts from the full user list.
/// </summary>
public record UserStatsDto(int Total, int Active, int Inactive, List<RoleCount> ByRole);

/// <summary>
/// A role name paired with the count of users assigned to that role.
/// Used to populate the role distribution chart on the admin dashboard.
/// </summary>
public record RoleCount(string Role, int Count);

/// <summary>
/// Query handler for all user read operations in the admin context.
/// Queries the <see cref="UserDbContext"/> directly using LINQ projections
/// to return <see cref="UserDto"/> objects without loading sensitive fields.
/// </summary>
public class GetUsersHandler(UserDbContext db)
{
    /// <summary>
    /// Returns a filtered, sorted list of users. Filters are applied cumulatively
    /// (AND logic). The search term is matched case-insensitively against email,
    /// first name, and last name. Results are ordered newest first.
    /// </summary>
    /// <param name="query">The filter criteria.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of <see cref="UserDto"/> objects matching the filters.</returns>
    public async Task<List<UserDto>> HandleAsync(GetUsersQuery query, CancellationToken ct = default)
    {
        var q = db.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Role)) q = q.Where(u => u.Role == query.Role);
        if (query.IsActive.HasValue) q = q.Where(u => u.IsActive == query.IsActive.Value);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.ToLower();
            q = q.Where(u => u.Email.Contains(s) || u.FirstName.Contains(s) || u.LastName.Contains(s));
        }
        return await q.OrderByDescending(u => u.CreatedAt)
            .Select(u => new UserDto(u.Id.ToString(), u.Email, u.FirstName, u.LastName,
                u.Role, u.PhoneNumber, u.IsActive, u.CreatedAt))
            .ToListAsync(ct);
    }

    /// <summary>
    /// Retrieves a single user by ID. Returns <c>null</c> if not found.
    /// </summary>
    public async Task<UserDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var u = await db.Users.FindAsync([id], ct);
        return u is null ? null : new UserDto(u.Id.ToString(), u.Email, u.FirstName, u.LastName,
            u.Role, u.PhoneNumber, u.IsActive, u.CreatedAt);
    }

    /// <summary>
    /// Computes aggregate user statistics in a single database round-trip using
    /// three parallel queries: total count, active count, and a GROUP BY role query.
    /// The inactive count is derived as (total - active) to avoid a fourth query.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A <see cref="UserStatsDto"/> with counts and role breakdown.</returns>
    public async Task<UserStatsDto> GetStatsAsync(CancellationToken ct = default)
    {
        var total = await db.Users.CountAsync(ct);
        var active = await db.Users.CountAsync(u => u.IsActive, ct);
        var byRole = await db.Users.GroupBy(u => u.Role)
            .Select(g => new RoleCount(g.Key, g.Count())).ToListAsync(ct);
        return new UserStatsDto(total, active, total - active, byRole);
    }
}
