using Microsoft.EntityFrameworkCore;
using UserService.Domain;

namespace UserService.Infrastructure;

// Connects to the same GroceryAuth DB as AuthService — read/write users for admin management
public class UserDbContext(DbContextOptions<UserDbContext> options) : DbContext(options)
{
    public DbSet<ManagedUser> Users => Set<ManagedUser>();

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
        });
    }
}
