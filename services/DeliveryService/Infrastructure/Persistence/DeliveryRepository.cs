using DeliveryService.Domain;
using Microsoft.EntityFrameworkCore;

namespace DeliveryService.Infrastructure.Persistence;

public interface IDeliveryRepository
{
    Task<Delivery?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Delivery?> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default);
    Task<IEnumerable<Delivery>> GetByDriverAsync(Guid driverId, CancellationToken ct = default);
    Task<IEnumerable<DeliverySlot>> GetSlotsByDateAsync(DateTime date, CancellationToken ct = default);
    Task AddAsync(Delivery delivery, CancellationToken ct = default);
    Task UpdateAsync(Delivery delivery, CancellationToken ct = default);
}

public class DeliveryRepository(DeliveryDbContext db) : IDeliveryRepository
{
    public Task<Delivery?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Deliveries.FirstOrDefaultAsync(d => d.Id == id, ct);

    public Task<Delivery?> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default) =>
        db.Deliveries.FirstOrDefaultAsync(d => d.OrderId == orderId, ct);

    public async Task<IEnumerable<Delivery>> GetByDriverAsync(Guid driverId, CancellationToken ct = default) =>
        await db.Deliveries.Where(d => d.DriverId == driverId).ToListAsync(ct);

    public async Task<IEnumerable<DeliverySlot>> GetSlotsByDateAsync(DateTime date, CancellationToken ct = default) =>
        await db.DeliverySlots
            .Where(s => s.StartTime.Date == date.Date && s.IsAvailable)
            .OrderBy(s => s.StartTime)
            .ToListAsync(ct);

    public async Task AddAsync(Delivery delivery, CancellationToken ct = default)
    {
        await db.Deliveries.AddAsync(delivery, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Delivery delivery, CancellationToken ct = default)
    {
        db.Deliveries.Update(delivery);
        await db.SaveChangesAsync(ct);
    }
}
