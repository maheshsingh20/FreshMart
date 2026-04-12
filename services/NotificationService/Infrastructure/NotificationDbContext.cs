using Microsoft.EntityFrameworkCore;
using NotificationService.Domain;

namespace NotificationService.Infrastructure;

/// <summary>
/// Entity Framework Core database context for the NotificationService.
/// Persists <see cref="Notification"/> records so customers can view their
/// notification history in the app (e.g. "Your order has been shipped").
/// Notifications are written when events are consumed from RabbitMQ or when
/// the HTTP fallback endpoints are called by other services.
/// </summary>
public class NotificationDbContext(DbContextOptions<NotificationDbContext> options) : DbContext(options)
{
    /// <summary>
    /// The notifications table. Each row represents a single notification
    /// delivered (or attempted) to a user, with type, title, and read status.
    /// </summary>
    public DbSet<Notification> Notifications => Set<Notification>();

    /// <summary>
    /// Configures the <see cref="Notification"/> entity mapping:
    /// <list type="bullet">
    ///   <item>Primary key on <c>Id</c>.</item>
    ///   <item>Max length constraints on <c>Title</c> and <c>Type</c> to prevent
    ///         oversized strings from event payloads.</item>
    ///   <item>Index on <c>UserId</c> for fast retrieval of a user's notification
    ///         history, which is the primary query pattern for the notification inbox.</item>
    /// </list>
    /// </summary>
    protected override void OnModelCreating(ModelBuilder m)
    {
        m.Entity<Notification>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).HasMaxLength(200);
            e.Property(x => x.Type).HasMaxLength(50);
            e.HasIndex(x => x.UserId);
        });
    }
}
