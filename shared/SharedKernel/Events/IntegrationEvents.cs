namespace SharedKernel.Events;

// Integration events published to message broker (Kafka/RabbitMQ)
public record OrderCreatedEvent(
    Guid OrderId,
    Guid CustomerId,
    decimal TotalAmount,
    List<OrderItemDto> Items,
    DateTime OccurredOn) : IIntegrationEvent, SharedKernel.Domain.IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public string EventType => "OrderCreated";
    // Denormalized for notification consumers (avoids cross-service lookup)
    public string CustomerEmail { get; init; } = "";
    public string CustomerFirstName { get; init; } = "";
    public string OrderRef { get; init; } = "";
}

public record PaymentCompletedEvent(
    Guid PaymentId,
    Guid OrderId,
    decimal Amount,
    string TransactionId,
    DateTime OccurredOn) : IIntegrationEvent, SharedKernel.Domain.IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public string EventType => "PaymentCompleted";
}

public record PaymentFailedEvent(
    Guid PaymentId,
    Guid OrderId,
    string Reason,
    DateTime OccurredOn) : IIntegrationEvent, SharedKernel.Domain.IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public string EventType => "PaymentFailed";
}

public record InventoryUpdatedEvent(
    Guid ProductId,
    int NewQuantity,
    int PreviousQuantity,
    DateTime OccurredOn) : IIntegrationEvent, SharedKernel.Domain.IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public string EventType => "InventoryUpdated";
}

public record DeliveryAssignedEvent(
    Guid DeliveryId,
    Guid OrderId,
    Guid DriverId,
    DateTime EstimatedDelivery,
    DateTime OccurredOn) : IIntegrationEvent, SharedKernel.Domain.IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public string EventType => "DeliveryAssigned";
}

public record OrderCancelledEvent(
    Guid OrderId,
    string Reason,
    DateTime OccurredOn) : IIntegrationEvent, SharedKernel.Domain.IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public string EventType => "OrderCancelled";
}

public record LowStockAlertEvent(
    Guid ProductId,
    string ProductName,
    int CurrentStock,
    int Threshold,
    DateTime OccurredOn) : IIntegrationEvent, SharedKernel.Domain.IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public string EventType => "LowStockAlert";
}

public record OrderStatusChangedEvent(
    Guid OrderId,
    Guid CustomerId,
    string OrderRef,
    string NewStatus,
    string CustomerEmail,
    string CustomerFirstName,
    DateTime OccurredOn,
    string DeliveryAddress = "",
    decimal TotalAmount = 0,
    decimal DeliveryFee = 0,
    decimal TaxAmount = 0,
    decimal DiscountAmount = 0,
    List<OrderItemDto>? Items = null) : IIntegrationEvent, SharedKernel.Domain.IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public string EventType => "OrderStatusChanged";
}

public record OtpRequestedEvent(
    Guid UserId,
    string Email,
    string FirstName,
    string Otp,
    string Purpose,   // "email-verification" | "password-reset"
    DateTime OccurredOn) : IIntegrationEvent, SharedKernel.Domain.IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public string EventType => "OtpRequested";
}

public record OrderItemDto(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice);

public interface IIntegrationEvent
{
    DateTime OccurredOn { get; }
}
