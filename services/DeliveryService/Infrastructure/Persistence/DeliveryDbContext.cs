using DeliveryService.Domain;
using Microsoft.EntityFrameworkCore;

namespace DeliveryService.Infrastructure.Persistence;

public class DeliveryDbContext(DbContextOptions<DeliveryDbContext> options) : DbContext(options)
{
    public DbSet<Delivery> Deliveries => Set<Delivery>();
    public DbSet<DeliverySlot> DeliverySlots => Set<DeliverySlot>();

    protected override void OnModelCreating(ModelBuilder m)
    {
        m.Entity<Delivery>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.DeliveryAddress).HasMaxLength(500).IsRequired();
            e.Property(x => x.Status).HasConversion<string>();
            e.Property(x => x.DeliveryNotes).HasMaxLength(500);
            e.Property(x => x.FailureReason).HasMaxLength(500);
            e.HasIndex(x => x.OrderId);
        });

        m.Entity<DeliverySlot>(e =>
        {
            e.HasKey(x => x.Id);
        });
    }
}
