using Microsoft.EntityFrameworkCore;
using SupportService.Infrastructure;

namespace SupportService.Application.Queries;

/// <summary>
/// Query to retrieve a list of support tickets.
/// The <c>IsStaff</c> flag controls visibility: staff see all tickets,
/// customers see only their own.
/// </summary>
public record GetTicketsQuery(Guid UserId, bool IsStaff);

/// <summary>
/// Query to retrieve a single support ticket by ID, including its message history.
/// The <c>IsStaff</c> flag and <c>UserId</c> are used together to enforce
/// ownership: a non-staff user can only access their own tickets.
/// </summary>
public record GetTicketByIdQuery(Guid TicketId, Guid UserId, bool IsStaff);

/// <summary>
/// Query handler for support ticket read operations.
/// Queries the <see cref="SupportDbContext"/> directly using LINQ projections
/// to return lightweight anonymous objects rather than full domain entities,
/// keeping the response payload small and avoiding unnecessary data loading.
/// </summary>
public class GetTicketsHandler(SupportDbContext db)
{
    /// <summary>
    /// Returns a list of tickets visible to the caller, ordered newest first.
    /// Staff receive all tickets; customers receive only their own.
    /// The response includes a <c>messageCount</c> field so the UI can show
    /// an unread indicator without loading the full message history.
    /// </summary>
    /// <param name="query">The query with user ID and staff flag.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// Always returns (true, null, tickets). The tuple shape is used for
    /// consistency with the command handlers.
    /// </returns>
    public async Task<(bool Success, string? Error, object? Result)> GetAllAsync(
        GetTicketsQuery query, CancellationToken ct = default)
    {
        var q = db.Tickets.AsQueryable();
        if (!query.IsStaff) q = q.Where(t => t.CustomerId == query.UserId);

        var tickets = await q.OrderByDescending(t => t.CreatedAt)
            .Select(t => new {
                id = t.Id, t.CustomerId, t.CustomerName, t.CustomerEmail,
                t.Subject, t.Category, t.Status, t.Priority,
                createdAt = t.CreatedAt, updatedAt = t.UpdatedAt, resolvedAt = t.ResolvedAt,
                messageCount = t.Messages.Count
            }).ToListAsync(ct);
        return (true, null, tickets);
    }

    /// <summary>
    /// Retrieves a single ticket with its full message history, ordered chronologically.
    /// Enforces ownership: if the caller is not staff and does not own the ticket,
    /// returns a "Forbidden" error so the controller can return 403.
    /// </summary>
    /// <param name="query">The query with ticket ID, user ID, and staff flag.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// (true, null, {ticket, messages}) on success.
    /// (false, "Not found", null) if the ticket does not exist.
    /// (false, "Forbidden", null) if the caller does not own the ticket.
    /// </returns>
    public async Task<(bool Success, string? Error, object? Result)> GetByIdAsync(
        GetTicketByIdQuery query, CancellationToken ct = default)
    {
        var ticket = await db.Tickets.Include(t => t.Messages)
            .FirstOrDefaultAsync(t => t.Id == query.TicketId, ct);
        if (ticket is null) return (false, "Not found", null);
        if (!query.IsStaff && ticket.CustomerId != query.UserId) return (false, "Forbidden", null);
        return (true, null, new { ticket, messages = ticket.Messages.OrderBy(m => m.CreatedAt) });
    }
}
