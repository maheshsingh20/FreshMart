using DeliveryService.Domain;
using SharedKernel.Domain;

namespace DeliveryService.Application;

/// <summary>
/// Application service contract for delivery lifecycle management.
/// Defines the operations available to the DeliveryService API layer and any
/// other services that need to interact with deliveries (e.g. OrderService
/// updating delivery status after a driver marks an order as delivered).
/// Implementations coordinate between the domain model and the persistence layer.
/// </summary>
public interface IDeliveryAppService
{
    /// <summary>
    /// Retrieves the delivery record associated with a specific order.
    /// Returns <c>null</c> if no delivery has been created for the order yet
    /// (e.g. the order is still being processed).
    /// </summary>
    /// <param name="orderId">The order whose delivery to look up.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<DeliveryDto?> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default);

    /// <summary>
    /// Assigns a delivery driver to a delivery and sets the estimated delivery time.
    /// This transitions the delivery from the unassigned state to the assigned state,
    /// enabling the driver to see it in their queue.
    /// </summary>
    /// <param name="deliveryId">The delivery to assign.</param>
    /// <param name="driverId">The driver's user ID.</param>
    /// <param name="eta">The estimated delivery date/time shown to the customer.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><see cref="Result.Success"/> or <see cref="Result.Failure"/> if the delivery is not found.</returns>
    Task<Result> AssignDriverAsync(Guid deliveryId, Guid driverId, DateTime eta, CancellationToken ct = default);

    /// <summary>
    /// Updates the driver's current GPS coordinates for a delivery.
    /// Called periodically by the driver app to enable real-time tracking on the
    /// customer-facing map. Silently no-ops if the delivery is not found to avoid
    /// errors from stale location pings.
    /// </summary>
    /// <param name="deliveryId">The delivery being tracked.</param>
    /// <param name="lat">Current latitude.</param>
    /// <param name="lng">Current longitude.</param>
    /// <param name="ct">Cancellation token.</param>
    Task UpdateLocationAsync(Guid deliveryId, double lat, double lng, CancellationToken ct = default);

    /// <summary>
    /// Advances the delivery to a new status (PickedUp, InTransit, Delivered, Failed).
    /// Each status transition is enforced by the domain model to prevent invalid
    /// state changes (e.g. marking a delivery as Delivered before it is PickedUp).
    /// </summary>
    /// <param name="deliveryId">The delivery to update.</param>
    /// <param name="status">The target status string (case-insensitive).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><see cref="Result.Success"/> or <see cref="Result.Failure"/> with an error message.</returns>
    Task<Result> UpdateStatusAsync(Guid deliveryId, string status, CancellationToken ct = default);

    /// <summary>
    /// Returns available delivery time slots for a given date.
    /// The available count per slot is computed as (MaxCapacity - CurrentBookings)
    /// so the frontend can show which slots still have capacity.
    /// </summary>
    /// <param name="date">The date to retrieve slots for.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<IEnumerable<SlotDto>> GetAvailableSlotsAsync(DateTime date, CancellationToken ct = default);
}

/// <summary>
/// Read model DTO representing a delivery record as returned by the API.
/// Includes the current GPS coordinates for real-time map tracking and
/// the three key timestamps: scheduled, estimated, and actual delivery.
/// </summary>
public record DeliveryDto(Guid Id, Guid OrderId, Guid? DriverId, string Status,
    string DeliveryAddress, double? Lat, double? Lng,
    DateTime? ScheduledAt, DateTime? EstimatedDelivery, DateTime? ActualDelivery);

/// <summary>
/// Read model DTO for a delivery time slot.
/// <c>Available</c> is the remaining capacity (MaxCapacity - CurrentBookings),
/// allowing the frontend to disable fully-booked slots in the slot picker UI.
/// </summary>
public record SlotDto(Guid Id, DateTime StartTime, DateTime EndTime, int Available);
