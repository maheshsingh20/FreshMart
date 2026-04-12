using Microsoft.EntityFrameworkCore;
using ReviewService.Domain;

namespace ReviewService.Infrastructure;

/// <summary>
/// Entity Framework Core database context for the ReviewService.
/// Manages persistence of <see cref="Review"/> entities in the dedicated
/// review database. The unique composite index on (ProductId, CustomerId)
/// enforces the one-review-per-customer-per-product business rule at the
/// database level as a safety net, complementing the application-layer check
/// in the command handler.
/// </summary>
public class ReviewDbContext(DbContextOptions<ReviewDbContext> options) : DbContext(options)
{
    /// <summary>
    /// The reviews table. Each row represents a single customer's review of a product.
    /// </summary>
    public DbSet<Review> Reviews => Set<Review>();

    /// <summary>
    /// Configures the <see cref="Review"/> entity mapping:
    /// <list type="bullet">
    ///   <item>Primary key on <c>Id</c>.</item>
    ///   <item>Max length constraint on <c>CustomerName</c> to prevent oversized strings.</item>
    ///   <item>Index on <c>ProductId</c> for fast retrieval of all reviews for a product.</item>
    ///   <item>Unique composite index on (ProductId, CustomerId) to enforce the
    ///         one-review-per-customer-per-product invariant at the database level.</item>
    /// </list>
    /// </summary>
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
