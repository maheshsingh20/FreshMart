using Microsoft.EntityFrameworkCore;
using SupportService.Infrastructure;

namespace SupportService.Application.Queries;

public record GetTicketsQuery(Guid UserId, bool IsStaff);
public record GetTicketByIdQuery(Guid TicketId, Guid UserId, bool IsStaff);

public class GetTicketsHandler(SupportDbContext db)
{
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
