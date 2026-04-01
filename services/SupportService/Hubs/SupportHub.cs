using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SupportService.Hubs;

/// <summary>
/// SignalR hub for real-time support ticket chat between customers and staff.
/// Clients join a group per ticket ID so messages are only delivered to participants
/// of that specific ticket — not broadcast to all connected users.
///
/// Connection URL: /hubs/support
/// Client events:
///   "ReceiveMessage" — a new chat message was sent.
///   "TicketUpdated"  — the ticket status changed (e.g. Resolved).
/// </summary>
[Authorize]
public class SupportHub : Hub
{
    /// <summary>
    /// Adds the caller's connection to the SignalR group for a specific ticket.
    /// Call this when the user opens a ticket detail page.
    /// </summary>
    /// <param name="ticketId">The ticket ID to join.</param>
    public async Task JoinTicket(string ticketId) =>
        await Groups.AddToGroupAsync(Context.ConnectionId, ticketId);

    /// <summary>
    /// Removes the caller's connection from a ticket's SignalR group.
    /// Call this when the user navigates away from the ticket detail page.
    /// </summary>
    /// <param name="ticketId">The ticket ID to leave.</param>
    public async Task LeaveTicket(string ticketId) =>
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, ticketId);
}
