namespace SharedKernel.Events;

// Integration events published to message broker (Kafka/RabbitMQ)
public record OrderCreatedEvent(
    Guid OrderId,
    Guid CustomerId,
    decimal TotalAmount,
    List<OrderItemDto> Items,
    DateTime OccurredOn) : IIntegrationEvent;

public record PaymentCompletedEvent(
    Guid PaymentId,
    Guid OrderId,
    decimal Amount,
    string TransactionId,
    DateTime OccurredOn) : IIntegrationEvent;

public record PaymentFailedEvent(
    Guid PaymentId,
    Guid OrderId,
    string Reason,
    DateTime OccurredOn) : IIntegrationEvent;

public record InventoryUpdatedEvent(
    Guid ProductId,
    int NewQuantity,
    int PreviousQuantity,
    DateTime OccurredOn) : IIntegrationEvent;

public record DeliveryAssignedEvent(
    Guid DeliveryId,
    Guid OrderId,
    Guid DriverId,
    DateTime EstimatedDelivery,
    DateTime OccurredOn) : IIntegrationEvent;

public record OrderCancelledEvent(
    Guid OrderId,
    string Reason,
    DateTime OccurredOn) : IIntegrationEvent;

public record LowStockAlertEvent(
    Guid ProductId,
    string ProductName,
    int CurrentStock,
    int Threshold,
    DateTime OccurredOn) : IIntegrationEvent;

public record OrderItemDto(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice);

public interface IIntegrationEvent
{
    DateTime OccurredOn { get; }
}
