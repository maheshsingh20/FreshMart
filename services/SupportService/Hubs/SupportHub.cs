using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SupportService.Hubs;

[Authorize]
public class SupportHub : Hub
{
    public async Task JoinTicket(string ticketId) =>
        await Groups.AddToGroupAsync(Context.ConnectionId, ticketId);

    public async Task LeaveTicket(string ticketId) =>
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, ticketId);
}
