using Backend.Data;
using Backend.Hubs;
using Backend.Models;
using Microsoft.AspNetCore.SignalR;

namespace Backend.Services;

public class NotificationService(AppDbContext db, IHubContext<NotificationHub> hub)
{
    public async Task SendToUserAsync(Guid userId, string title, string message, string type = "info", string? link = null)
    {
        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            Link = link
        };
        db.Notifications.Add(notification);
        await db.SaveChangesAsync();

        var payload = ToPayload(notification);
        await hub.Clients.Group($"user:{userId}").SendAsync("notification", payload);
    }

    public async Task SendToRoleAsync(string role, string title, string message, string type = "info", string? link = null)
    {
        // For role broadcasts we don't persist per-user — just push live
        var payload = new
        {
            id = Guid.NewGuid().ToString(),
            title,
            message,
            type,
            link,
            isRead = false,
            createdAt = DateTime.UtcNow.ToString("o")
        };
        await hub.Clients.Group($"role:{role}").SendAsync("notification", payload);
    }

    private static object ToPayload(Notification n) => new
    {
        id = n.Id.ToString(),
        title = n.Title,
        message = n.Message,
        type = n.Type,
        link = n.Link,
        isRead = n.IsRead,
        createdAt = n.CreatedAt.ToString("o")
    };
}
