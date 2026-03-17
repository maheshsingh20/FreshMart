using Microsoft.EntityFrameworkCore;
using PaymentService.Domain;

namespace PaymentService.Infrastructure.Persistence;

public class PaymentDbContext(DbContextOptions<PaymentDbContext> options) : DbContext(options)
{
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder m)
    {
        m.Entity<Payment>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.Status).HasConversion<string>();
            e.Property(x => x.Method).HasConversion<string>();
            e.Property(x => x.StripePaymentIntentId).HasMaxLength(200);
            e.Property(x => x.StripeChargeId).HasMaxLength(200);
            e.HasIndex(x => x.OrderId);
        });
    }
}
