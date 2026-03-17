using SharedKernel.Domain;
using SharedKernel.Events;

namespace DeliveryService.Domain;

public enum DeliveryStatus
{
    Pending, Assigned, PickedUp, InTransit, Delivered, Failed, Cancelled
}

public class Delivery : AggregateRoot
{
    public Guid OrderId { get; private set; }
    public Guid? DriverId { get; private set; }
    public DeliveryStatus Status { get; private set; } = DeliveryStatus.Pending;
    public string DeliveryAddress { get; private set; } = default!;
    public double? CurrentLatitude { get; private set; }
    public double? CurrentLongitude { get; private set; }
    public DateTime? ScheduledAt { get; private set; }
    public DateTime? EstimatedDelivery { get; private set; }
    public DateTime? ActualDelivery { get; private set; }
    public string? DeliveryNotes { get; private set; }
    public string? FailureReason { get; private set; }

    private Delivery() { }

    public static Delivery Create(Guid orderId, string address, DateTime? scheduledAt = null) =>
        new()
        {
            OrderId = orderId,
            DeliveryAddress = address,
            ScheduledAt = scheduledAt ?? DateTime.UtcNow.AddHours(2)
        };

    public void AssignDriver(Guid driverId, DateTime estimatedDelivery)
    {
        DriverId = driverId;
        Status = DeliveryStatus.Assigned;
        EstimatedDelivery = estimatedDelivery;
        SetUpdated();
        AddDomainEvent(new DeliveryAssignedEvent(Id, OrderId, driverId, estimatedDelivery, DateTime.UtcNow));
    }

    public void UpdateLocation(double lat, double lng)
    {
        CurrentLatitude = lat;
        CurrentLongitude = lng;
        SetUpdated();
    }

    public void PickUp() { Status = DeliveryStatus.PickedUp; SetUpdated(); }
    public void StartTransit() { Status = DeliveryStatus.InTransit; SetUpdated(); }

    public void Complete()
    {
        Status = DeliveryStatus.Delivered;
        ActualDelivery = DateTime.UtcNow;
        SetUpdated();
    }

    public void Fail(string reason)
    {
        Status = DeliveryStatus.Failed;
        FailureReason = reason;
        SetUpdated();
    }
}

public class DeliverySlot : Entity
{
    public DateTime StartTime { get; private set; }
    public DateTime EndTime { get; private set; }
    public int MaxCapacity { get; private set; }
    public int CurrentBookings { get; private set; }
    public bool IsAvailable => CurrentBookings < MaxCapacity;

    private DeliverySlot() { }

    public static DeliverySlot Create(DateTime start, DateTime end, int capacity) =>
        new() { StartTime = start, EndTime = end, MaxCapacity = capacity };

    public void Book() { if (IsAvailable) CurrentBookings++; }
}
