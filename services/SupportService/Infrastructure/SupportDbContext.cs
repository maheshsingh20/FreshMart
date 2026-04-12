using Microsoft.EntityFrameworkCore;
using SupportService.Domain;

namespace SupportService.Infrastructure;

/// <summary>
/// Entity Framework Core database context for the SupportService.
/// Manages persistence of <see cref="SupportTicket"/> and <see cref="SupportMessage"/>
/// entities. The schema is designed to support efficient ticket listing (index on
/// <c>CustomerId</c>) and message retrieval (foreign key from message to ticket).
/// </summary>
public class SupportDbContext(DbContextOptions<SupportDbContext> options) : DbContext(options)
{
    /// <summary>
    /// The support tickets table. Each row represents a customer's support request,
    /// with status, priority, and category fields for triage and SLA tracking.
    /// </summary>
    public DbSet<SupportTicket> Tickets => Set<SupportTicket>();

    /// <summary>
    /// The support messages table. Each row is a single message in a ticket's
    /// conversation thread, sent by either the customer or a staff member.
    /// </summary>
    public DbSet<SupportMessage> Messages => Set<SupportMessage>();

    /// <summary>
    /// Configures entity mappings and constraints:
    /// <list type="bullet">
    ///   <item><see cref="SupportTicket"/>: primary key, string length limits, one-to-many
    ///         relationship to messages, and an index on <c>CustomerId</c> for fast
    ///         per-customer ticket queries.</item>
    ///   <item><see cref="SupportMessage"/>: primary key only — messages are always
    ///         accessed via the ticket relationship.</item>
    /// </list>
    /// </summary>
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
