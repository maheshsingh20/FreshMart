using Microsoft.EntityFrameworkCore;
using ReviewService.Domain;

namespace ReviewService.Infrastructure;

public class ReviewDbContext(DbContextOptions<ReviewDbContext> options) : DbContext(options)
{
    public DbSet<Review> Reviews => Set<Review>();

    protected override void OnModelCreating(ModelBuilder m)
    {
        m.Entity<Review>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.CustomerName).HasMaxLength(200);
            e.HasIndex(x => x.ProductId);
            e.HasIndex(x => new { x.ProductId, x.CustomerId }).IsUnique();
        });
    }
}
