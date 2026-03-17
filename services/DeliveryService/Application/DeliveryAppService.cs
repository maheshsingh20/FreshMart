using DeliveryService.Domain;
using DeliveryService.Infrastructure.Persistence;
using SharedKernel.Domain;

namespace DeliveryService.Application;

public class DeliveryAppService(IDeliveryRepository repo) : IDeliveryAppService
{
    public async Task<DeliveryDto?> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default)
    {
        var d = await repo.GetByOrderIdAsync(orderId, ct);
        return d is null ? null : MapToDto(d);
    }

    public async Task<Result> AssignDriverAsync(
        Guid deliveryId, Guid driverId, DateTime eta, CancellationToken ct = default)
    {
        var delivery = await repo.GetByIdAsync(deliveryId, ct);
        if (delivery is null) return Result.Failure("Delivery not found.");
        delivery.AssignDriver(driverId, eta);
        await repo.UpdateAsync(delivery, ct);
        return Result.Success();
    }

    public async Task UpdateLocationAsync(
        Guid deliveryId, double lat, double lng, CancellationToken ct = default)
    {
        var delivery = await repo.GetByIdAsync(deliveryId, ct);
        if (delivery is null) return;
        delivery.UpdateLocation(lat, lng);
        await repo.UpdateAsync(delivery, ct);
    }

    public async Task<Result> UpdateStatusAsync(
        Guid deliveryId, string status, CancellationToken ct = default)
    {
        var delivery = await repo.GetByIdAsync(deliveryId, ct);
        if (delivery is null) return Result.Failure("Delivery not found.");

        switch (status.ToLower())
        {
            case "pickedup":   delivery.PickUp(); break;
            case "intransit":  delivery.StartTransit(); break;
            case "delivered":  delivery.Complete(); break;
            case "failed":     delivery.Fail("Delivery failed"); break;
            default: return Result.Failure($"Unknown status: {status}");
        }

        await repo.UpdateAsync(delivery, ct);
        return Result.Success();
    }

    public async Task<IEnumerable<SlotDto>> GetAvailableSlotsAsync(
        DateTime date, CancellationToken ct = default)
    {
        var slots = await repo.GetSlotsByDateAsync(date, ct);
        return slots.Select(s => new SlotDto(s.Id, s.StartTime, s.EndTime,
            s.MaxCapacity - s.CurrentBookings));
    }

    private static DeliveryDto MapToDto(Delivery d) =>
        new(d.Id, d.OrderId, d.DriverId, d.Status.ToString(), d.DeliveryAddress,
            d.CurrentLatitude, d.CurrentLongitude,
            d.ScheduledAt, d.EstimatedDelivery, d.ActualDelivery);
}
