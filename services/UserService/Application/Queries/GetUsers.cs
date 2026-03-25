using Microsoft.EntityFrameworkCore;
using UserService.Infrastructure;

namespace UserService.Application.Queries;

public record GetUsersQuery(string? Role, string? Search, bool? IsActive);
public record GetUserByIdQuery(Guid Id);
public record GetUserStatsQuery();

public record UserDto(
    string Id, string Email, string FirstName, string LastName,
    string Role, string? PhoneNumber, bool IsActive, DateTime CreatedAt);

public record UserStatsDto(int Total, int Active, int Inactive, List<RoleCount> ByRole);
public record RoleCount(string Role, int Count);

public class GetUsersHandler(UserDbContext db)
{
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

    public async Task<UserDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var u = await db.Users.FindAsync([id], ct);
        return u is null ? null : new UserDto(u.Id.ToString(), u.Email, u.FirstName, u.LastName,
            u.Role, u.PhoneNumber, u.IsActive, u.CreatedAt);
    }

    public async Task<UserStatsDto> GetStatsAsync(CancellationToken ct = default)
    {
        var total = await db.Users.CountAsync(ct);
        var active = await db.Users.CountAsync(u => u.IsActive, ct);
        var byRole = await db.Users.GroupBy(u => u.Role)
            .Select(g => new RoleCount(g.Key, g.Count())).ToListAsync(ct);
        return new UserStatsDto(total, active, total - active, byRole);
    }
}
