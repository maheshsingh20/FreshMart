using AuthService.Domain;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Persistence;

public class AuthDbContext(DbContextOptions<AuthDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Address> Addresses => Set<Address>();

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
