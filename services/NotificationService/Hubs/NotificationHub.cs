using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace NotificationService.Hubs;

/// <summary>
/// SignalR hub that delivers real-time notifications to connected browser clients.
/// Each authenticated user is automatically added to a group named after their user ID
/// so <see cref="EventConsumerService"/> can push to a specific user without broadcasting to everyone.
///
/// Connection URL: /hubs/notifications
/// Client event: "notification" — payload is a <see cref="NotificationService.Domain.Notification"/> object.
///
/// The JWT token is passed via the "access_token" query parameter for SignalR WebSocket connections
/// (configured in NotificationService/Program.cs JwtBearerEvents.OnMessageReceived).
/// </summary>
[Authorize]
public class NotificationHub : Hub
{
    /// <summary>
    /// Called when a client connects. Adds the connection to the user's personal group
    /// so targeted pushes work correctly even when the user has multiple browser tabs open.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (userId != null)
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);
        await base.OnConnectedAsync();
    }
}
