using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SupportService.Domain;
using SupportService.Hubs;
using SupportService.Infrastructure;

namespace SupportService.Application.Commands;

/// <summary>
/// Command to open a new support ticket.
/// Carries the customer's identity (ID, name, email), the ticket subject,
/// optional category and description, and the priority level.
/// </summary>
public record CreateTicketCommand(
    Guid CustomerId, string CustomerName, string CustomerEmail,
    string Subject, string? Category, string? Description, string? Priority);

/// <summary>
/// Command to send a message in an existing ticket's conversation thread.
/// The <c>IsStaff</c> flag determines whether the message is displayed as a
/// staff reply (which also triggers the InProgress status transition).
/// </summary>
public record SendMessageCommand(
    Guid TicketId, Guid SenderId, string SenderName,
    string SenderRole, string Message, bool IsStaff);

/// <summary>
/// Command to update the status of a support ticket.
/// Valid status values are typically: Open, InProgress, Resolved, Closed.
/// </summary>
public record UpdateTicketStatusCommand(Guid TicketId, string Status);

/// <summary>
/// Application service that handles all support ticket write operations.
/// Combines database persistence (via <see cref="SupportDbContext"/>) with
/// real-time SignalR notifications (via <see cref="IHubContext{SupportHub}"/>)
/// so that both the REST API response and the live chat UI stay in sync.
/// </summary>
public class TicketCommandHandler(SupportDbContext db, IHubContext<SupportHub> hub)
{
    /// <summary>
    /// Creates a new support ticket and optionally adds the description as the
    /// first message in the conversation thread. Defaults category to "General"
    /// and priority to "Medium" when not specified, ensuring the ticket is always
    /// in a valid state for staff to triage.
    /// </summary>
    /// <param name="cmd">The create ticket command.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A tuple of (Success, Result) where Result is the persisted ticket entity.</returns>
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

    /// <summary>
    /// Appends a message to an existing ticket's conversation thread and broadcasts
    /// it to all SignalR clients subscribed to the ticket's group.
    /// Automatically advances the ticket from "Open" to "InProgress" when a staff
    /// member sends the first reply, signalling that the issue is being worked on.
    /// </summary>
    /// <param name="cmd">The send message command.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// (true, null, message) on success, or (false, errorMessage, null) if the ticket is not found.
    /// </returns>
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

    /// <summary>
    /// Updates the status of a ticket and broadcasts the change via SignalR so
    /// the customer's support chat UI reflects the new status in real time.
    /// Records the <c>ResolvedAt</c> timestamp when the status is set to "Resolved"
    /// for SLA reporting and customer communication purposes.
    /// </summary>
    /// <param name="cmd">The update status command.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>(true, null) on success, or (false, errorMessage) if the ticket is not found.</returns>
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
