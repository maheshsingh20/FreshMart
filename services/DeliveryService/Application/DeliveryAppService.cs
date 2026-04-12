using DeliveryService.Domain;
using DeliveryService.Infrastructure.Persistence;
using SharedKernel.Domain;

namespace DeliveryService.Application;

/// <summary>
/// Concrete implementation of <see cref="IDeliveryAppService"/>.
/// Orchestrates delivery lifecycle operations by loading domain aggregates from
/// the repository, invoking domain methods that enforce business rules, and
/// persisting the updated state. All domain state transitions (AssignDriver,
/// PickUp, StartTransit, Complete, Fail) are delegated to the <see cref="Delivery"/>
/// aggregate to keep business logic out of the application layer.
/// </summary>
public class DeliveryAppService(IDeliveryRepository repo) : IDeliveryAppService
{
    /// <summary>
    /// Looks up the delivery for the given order and maps it to a DTO.
    /// Returns <c>null</c> if no delivery record exists for the order.
    /// </summary>
    public async Task<DeliveryDto?> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default)
    {
        var d = await repo.GetByOrderIdAsync(orderId, ct);
        return d is null ? null : MapToDto(d);
    }

    /// <summary>
    /// Assigns a driver to the delivery and sets the ETA by delegating to the
    /// domain aggregate's <c>AssignDriver</c> method, which enforces that the
    /// delivery must be in an assignable state.
    /// </summary>
    public async Task<Result> AssignDriverAsync(
        Guid deliveryId, Guid driverId, DateTime eta, CancellationToken ct = default)
    {
        var delivery = await repo.GetByIdAsync(deliveryId, ct);
        if (delivery is null) return Result.Failure("Delivery not found.");
        delivery.AssignDriver(driverId, eta);
        await repo.UpdateAsync(delivery, ct);
        return Result.Success();
    }

    /// <summary>
    /// Updates the delivery's GPS coordinates. Silently returns if the delivery
    /// is not found — location pings from the driver app may arrive after a
    /// delivery is completed or the record is cleaned up.
    /// </summary>
    public async Task UpdateLocationAsync(
        Guid deliveryId, double lat, double lng, CancellationToken ct = default)
    {
        var delivery = await repo.GetByIdAsync(deliveryId, ct);
        if (delivery is null) return;
        delivery.UpdateLocation(lat, lng);
        await repo.UpdateAsync(delivery, ct);
    }

    /// <summary>
    /// Maps the status string to the appropriate domain method call.
    /// The switch statement acts as an anti-corruption layer between the
    /// string-based API contract and the strongly-typed domain model.
    /// Returns a failure result for unrecognised status strings rather than
    /// throwing, so the API can return a 400 with a descriptive message.
    /// </summary>
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

    /// <summary>
    /// Retrieves available delivery slots for the given date and computes the
    /// remaining capacity for each slot. Slots with zero remaining capacity are
    /// still returned so the frontend can show them as unavailable (greyed out)
    /// rather than hiding them entirely.
    /// </summary>
    public async Task<IEnumerable<SlotDto>> GetAvailableSlotsAsync(
        DateTime date, CancellationToken ct = default)
    {
        var slots = await repo.GetSlotsByDateAsync(date, ct);
        return slots.Select(s => new SlotDto(s.Id, s.StartTime, s.EndTime,
            s.MaxCapacity - s.CurrentBookings));
    }

    /// <summary>
    /// Maps a <see cref="Delivery"/> domain aggregate to a <see cref="DeliveryDto"/>
    /// read model. Kept private to this class — the mapping is an implementation
    /// detail of the application service.
    /// </summary>
    private static DeliveryDto MapToDto(Delivery d) =>
        new(d.Id, d.OrderId, d.DriverId, d.Status.ToString(), d.DeliveryAddress,
            d.CurrentLatitude, d.CurrentLongitude,
            d.ScheduledAt, d.EstimatedDelivery, d.ActualDelivery);
}
