using Microsoft.EntityFrameworkCore;
using OrderService.Domain;

namespace OrderService.Infrastructure.Persistence;

public class OrderRepository(OrderDbContext db) : IOrderRepository
{
    public Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<IEnumerable<Order>> GetByCustomerAsync(
        Guid customerId, int page, int pageSize, CancellationToken ct = default) =>
        await db.Orders.Include(o => o.Items)
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);

    public async Task<IEnumerable<Order>> GetByStatusAsync(
        OrderStatus status, CancellationToken ct = default) =>
        await db.Orders.Include(o => o.Items)
            .Where(o => o.Status == status).ToListAsync(ct);

    public Task<int> GetTotalByCustomerAsync(Guid customerId, CancellationToken ct = default) =>
        db.Orders.CountAsync(o => o.CustomerId == customerId, ct);

    public async Task AddAsync(Order order, CancellationToken ct = default)
    {
        await db.Orders.AddAsync(order, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Order order, CancellationToken ct = default)
    {
        db.Orders.Update(order);
        await db.SaveChangesAsync(ct);
    }
}
