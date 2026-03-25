using CouponService.Domain;
using Microsoft.EntityFrameworkCore;

namespace CouponService.Infrastructure;

public class CouponDbContext(DbContextOptions<CouponDbContext> options) : DbContext(options)
{
    public DbSet<Coupon> Coupons => Set<Coupon>();

    protected override void OnModelCreating(ModelBuilder m)
    {
        m.Entity<Coupon>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(50);
            e.Property(x => x.DiscountValue).HasPrecision(18, 2);
            e.Property(x => x.MinOrderAmount).HasPrecision(18, 2);
            e.HasIndex(x => x.Code).IsUnique();
        });
    }
}
