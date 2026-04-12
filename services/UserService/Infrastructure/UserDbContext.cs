using Microsoft.EntityFrameworkCore;
using UserService.Domain;

namespace UserService.Infrastructure;

/// <summary>
/// Entity Framework Core database context for the UserService.
/// Connects to the same <c>GroceryAuth</c> database as AuthService and maps
/// to the same <c>Users</c> table, giving the admin UserService read/write
/// access to user records without duplicating data.
/// This shared-database pattern is intentional: user identity is owned by
/// AuthService, but admin management operations (role changes, deactivation,
/// deletion) are surfaced through UserService to keep the AuthService API
/// focused on authentication concerns.
/// </summary>
// Connects to the same GroceryAuth DB as AuthService — read/write users for admin management
public class UserDbContext(DbContextOptions<UserDbContext> options) : DbContext(options)
{
    /// <summary>
    /// The users table, shared with AuthService. Exposes the <see cref="ManagedUser"/>
    /// projection which includes admin-relevant fields (role, active status, refresh token)
    /// but excludes the password hash.
    /// </summary>
    public DbSet<ManagedUser> Users => Set<ManagedUser>();

    /// <summary>
    /// Maps <see cref="ManagedUser"/> to the <c>Users</c> table with the same column
    /// definitions as AuthService's <c>AuthDbContext</c>. The <c>RefreshToken</c> column
    /// is mapped so the UserService can revoke tokens when deactivating or deleting accounts.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder m)
    {
        m.Entity<ManagedUser>(e =>
        {
            e.ToTable("Users"); // same table as AuthService
            e.HasKey(x => x.Id);
            e.Property(x => x.Email).HasMaxLength(256);
            e.Property(x => x.FirstName).HasMaxLength(100);
            e.Property(x => x.LastName).HasMaxLength(100);
            e.Property(x => x.PhoneNumber).HasMaxLength(20);
            e.Property(x => x.Role).HasMaxLength(50);
            e.Property(x => x.RefreshToken).HasMaxLength(512);
        });
    }
}
