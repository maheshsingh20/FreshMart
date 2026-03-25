using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SupportService.Domain;
using SupportService.Hubs;
using SupportService.Infrastructure;

namespace SupportService.Application.Commands;

public record CreateTicketCommand(
    Guid CustomerId, string CustomerName, string CustomerEmail,
    string Subject, string? Category, string? Description, string? Priority);

public record SendMessageCommand(
    Guid TicketId, Guid SenderId, string SenderName,
    string SenderRole, string Message, bool IsStaff);

public record UpdateTicketStatusCommand(Guid TicketId, string Status);

public class TicketCommandHandler(SupportDbContext db, IHubContext<SupportHub> hub)
{
    public async Task<(bool Success, object? Result)> CreateAsync(
        CreateTicketCommand cmd, CancellationToken ct = default)
    {
        var ticket = new SupportTicket
        {
            CustomerId = cmd.CustomerId,
            CustomerName = cmd.CustomerName,
            CustomerEmail = cmd.CustomerEmail,
            Subject = cmd.Subject,
            Category = cmd.Category ?? "General",
            Priority = cmd.Priority ?? "Medium"
        };
        if (!string.IsNullOrWhiteSpace(cmd.Description))
            ticket.Messages.Add(new SupportMessage
            {
                TicketId = ticket.Id, SenderId = cmd.CustomerId,
                SenderName = cmd.CustomerName, SenderRole = "Customer",
                Message = cmd.Description, IsStaff = false
            });

        db.Tickets.Add(ticket);
        await db.SaveChangesAsync(ct);
        return (true, ticket);
    }

    public async Task<(bool Success, string? Error, object? Result)> SendMessageAsync(
        SendMessageCommand cmd, CancellationToken ct = default)
    {
        var ticket = await db.Tickets.FindAsync([cmd.TicketId], ct);
        if (ticket is null) return (false, "Ticket not found", null);

        var msg = new SupportMessage
        {
            TicketId = cmd.TicketId, SenderId = cmd.SenderId,
            SenderName = cmd.SenderName, SenderRole = cmd.SenderRole,
            Message = cmd.Message, IsStaff = cmd.IsStaff
        };
        db.Messages.Add(msg);
        ticket.UpdatedAt = DateTime.UtcNow;
        if (cmd.IsStaff && ticket.Status == "Open") ticket.Status = "InProgress";
        await db.SaveChangesAsync(ct);

        await hub.Clients.Group(cmd.TicketId.ToString()).SendAsync("newMessage", msg, ct);
        return (true, null, msg);
    }

    public async Task<(bool Success, string? Error)> UpdateStatusAsync(
        UpdateTicketStatusCommand cmd, CancellationToken ct = default)
    {
        var ticket = await db.Tickets.FindAsync([cmd.TicketId], ct);
        if (ticket is null) return (false, "Ticket not found");
        ticket.Status = cmd.Status;
        ticket.UpdatedAt = DateTime.UtcNow;
        if (cmd.Status == "Resolved") ticket.ResolvedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await hub.Clients.Group(cmd.TicketId.ToString())
            .SendAsync("ticketUpdated", new { ticket.Id, ticket.Status }, ct);
        return (true, null);
    }
}
