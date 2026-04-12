using AuthService.Domain;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Persistence;

/// <summary>
/// Entity Framework Core database context for the AuthService.
/// Owns the <c>Users</c> and <c>Addresses</c> tables in the <c>GroceryAuth</c> database.
/// This is the authoritative source of user identity data — UserService connects
/// to the same database (read/write) for admin management, but AuthService owns
/// the schema and migrations.
/// </summary>
public class AuthDbContext(DbContextOptions<AuthDbContext> options) : DbContext(options)
{
    /// <summary>
    /// The users table. Each row represents a registered user account with
    /// credentials, role, and token state.
    /// </summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>
    /// The addresses table. Each row is a saved delivery address belonging to a user.
    /// A user can have multiple addresses; one is designated as the default.
    /// </summary>
    public DbSet<Address> Addresses => Set<Address>();

    /// <summary>
    /// Configures entity mappings, constraints, and indexes:
    /// <list type="bullet">
    ///   <item><see cref="User"/>: unique index on email (case-sensitive at DB level,
    ///         normalised to lowercase in application code), index on GoogleId for
    ///         fast OAuth lookups, string length limits on all text fields, and
    ///         enum-to-string conversion for <c>Role</c> to store readable values.</item>
    ///   <item><see cref="Address"/>: index on UserId for fast per-user address queries,
    ///         string length limits, and a default value of "India" for Country.</item>
    /// </list>
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Email).HasMaxLength(256).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.PasswordHash).IsRequired();
            e.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            e.Property(x => x.LastName).HasMaxLength(100).IsRequired();
            e.Property(x => x.PhoneNumber).HasMaxLength(20);
            e.Property(x => x.Role).HasConversion<string>();
            e.Property(x => x.RefreshToken).HasMaxLength(512);
            e.Property(x => x.GoogleId).HasMaxLength(128);
            e.HasIndex(x => x.GoogleId);
            e.Property(x => x.OtpHash).HasMaxLength(512);
            e.Property(x => x.OtpPurpose).HasMaxLength(50);
        });

        modelBuilder.Entity<Address>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Label).HasMaxLength(50).IsRequired();
            e.Property(x => x.Line1).HasMaxLength(300).IsRequired();
            e.Property(x => x.Line2).HasMaxLength(200);
            e.Property(x => x.City).HasMaxLength(100).IsRequired();
            e.Property(x => x.State).HasMaxLength(100).IsRequired();
            e.Property(x => x.Pincode).HasMaxLength(20).IsRequired();
            e.Property(x => x.Country).HasMaxLength(100).HasDefaultValue("India");
            e.HasIndex(x => x.UserId);
        });
    }
}
