using DeliveryService.Domain;
using SharedKernel.Domain;

namespace DeliveryService.Application;

public interface IDeliveryAppService
{
    Task<DeliveryDto?> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default);
    Task<Result> AssignDriverAsync(Guid deliveryId, Guid driverId, DateTime eta, CancellationToken ct = default);
    Task UpdateLocationAsync(Guid deliveryId, double lat, double lng, CancellationToken ct = default);
    Task<Result> UpdateStatusAsync(Guid deliveryId, string status, CancellationToken ct = default);
    Task<IEnumerable<SlotDto>> GetAvailableSlotsAsync(DateTime date, CancellationToken ct = default);
}

public record DeliveryDto(Guid Id, Guid OrderId, Guid? DriverId, string Status,
    string DeliveryAddress, double? Lat, double? Lng,
    DateTime? ScheduledAt, DateTime? EstimatedDelivery, DateTime? ActualDelivery);

public record SlotDto(Guid Id, DateTime StartTime, DateTime EndTime, int Available);
