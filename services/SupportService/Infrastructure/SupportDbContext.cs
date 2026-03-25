using Microsoft.EntityFrameworkCore;
using SupportService.Domain;

namespace SupportService.Infrastructure;

public class SupportDbContext(DbContextOptions<SupportDbContext> options) : DbContext(options)
{
    public DbSet<SupportTicket> Tickets => Set<SupportTicket>();
    public DbSet<SupportMessage> Messages => Set<SupportMessage>();

    protected override void OnModelCreating(ModelBuilder m)
    {
        m.Entity<SupportTicket>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Subject).HasMaxLength(300);
            e.Property(x => x.Status).HasMaxLength(50);
            e.Property(x => x.Priority).HasMaxLength(50);
            e.Property(x => x.Category).HasMaxLength(100);
            e.HasMany(x => x.Messages).WithOne().HasForeignKey(x => x.TicketId);
            e.HasIndex(x => x.CustomerId);
        });
        m.Entity<SupportMessage>(e =>
        {
            e.HasKey(x => x.Id);
        });
    }
}
