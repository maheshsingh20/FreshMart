namespace SharedKernel.Events;

/// <summary>
/// Raised when a new order is placed and persisted.
/// Consumed by PaymentService (to initiate payment) and NotificationService (to send confirmation email).
/// Customer fields are denormalized here to avoid cross-service lookups at consumption time.
/// </summary>
public record OrderCreatedEvent(
    Guid OrderId,
    Guid CustomerId,
    decimal TotalAmount,
    List<OrderItemDto> Items,
    DateTime OccurredOn) : IIntegrationEvent, SharedKernel.Domain.IDomainEvent
{
    /// <inheritdoc/>
    public Guid EventId { get; } = Guid.NewGuid();

    /// <inheritdoc/>
    public string EventType => "OrderCreated";

    /// <summary>Customer email — denormalized so NotificationService can send without calling AuthService.</summary>
    public string CustomerEmail { get; init; } = "";

    /// <summary>Customer first name — used for personalised notification messages.</summary>
    public string CustomerFirstName { get; init; } = "";

    /// <summary>Human-readable order reference shown in emails and the UI.</summary>
    public string OrderRef { get; init; } = "";
}

/// <summary>
/// Raised by PaymentService when a payment is successfully captured.
/// Consumed by OrderService (to advance order status) and NotificationService (to send receipt).
/// </summary>
public record PaymentCompletedEvent(
    Guid PaymentId,
    Guid OrderId,
    decimal Amount,
    string TransactionId,
    DateTime OccurredOn) : IIntegrationEvent, SharedKernel.Domain.IDomainEvent
{
    /// <inheritdoc/>
    public Guid EventId { get; } = Guid.NewGuid();

    /// <inheritdoc/>
    public string EventType => "PaymentCompleted";
}

/// <summary>
/// Raised by PaymentService when a payment attempt fails.
/// Consumed by OrderService (to revert order to pending/cancelled) and NotificationService.
/// </summary>
public record PaymentFailedEvent(
    Guid PaymentId,
    Guid OrderId,
    string Reason,
    DateTime OccurredOn) : IIntegrationEvent, SharedKernel.Domain.IDomainEvent
{
    /// <inheritdoc/>
    public Guid EventId { get; } = Guid.NewGuid();

    /// <inheritdoc/>
    public string EventType => "PaymentFailed";
}

/// <summary>
/// Raised by ProductService when stock levels change (purchase, restock, or manual adjustment).
/// Consumed by any service that needs to react to inventory changes (e.g. low-stock alerts).
/// </summary>
public record InventoryUpdatedEvent(
    Guid ProductId,
    int NewQuantity,
    int PreviousQuantity,
    DateTime OccurredOn) : IIntegrationEvent, SharedKernel.Domain.IDomainEvent
{
    /// <inheritdoc/>
    public Guid EventId { get; } = Guid.NewGuid();

    /// <inheritdoc/>
    public string EventType => "InventoryUpdated";
}

/// <summary>
/// Raised by DeliveryService when a driver is assigned to an order.
/// Consumed by NotificationService to inform the customer of their delivery driver.
/// </summary>
public record DeliveryAssignedEvent(
    Guid DeliveryId,
    Guid OrderId,
    Guid DriverId,
    DateTime EstimatedDelivery,
    DateTime OccurredOn) : IIntegrationEvent, SharedKernel.Domain.IDomainEvent
{
    /// <inheritdoc/>
    public Guid EventId { get; } = Guid.NewGuid();

    /// <inheritdoc/>
    public string EventType => "DeliveryAssigned";
}

/// <summary>
/// Raised when an order is cancelled (by customer or system).
/// Consumed by PaymentService (to trigger refund) and NotificationService.
/// </summary>
public record OrderCancelledEvent(
    Guid OrderId,
    string Reason,
    DateTime OccurredOn) : IIntegrationEvent, SharedKernel.Domain.IDomainEvent
{
    /// <inheritdoc/>
    public Guid EventId { get; } = Guid.NewGuid();

    /// <inheritdoc/>
    public string EventType => "OrderCancelled";
}

/// <summary>
/// Raised when a product's stock falls below its configured threshold.
/// Consumed by NotificationService to alert store managers.
/// </summary>
public record LowStockAlertEvent(
    Guid ProductId,
    string ProductName,
    int CurrentStock,
    int Threshold,
    DateTime OccurredOn) : IIntegrationEvent, SharedKernel.Domain.IDomainEvent
{
    /// <inheritdoc/>
    public Guid EventId { get; } = Guid.NewGuid();

    /// <inheritdoc/>
    public string EventType => "LowStockAlert";
}

/// <summary>
/// Raised whenever an order transitions to a new status (e.g. Confirmed → Preparing → OutForDelivery).
/// Consumed by NotificationService to send real-time status update emails/push notifications.
/// All customer and order fields are denormalized to avoid cross-service calls at consumption time.
/// </summary>
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
    /// <inheritdoc/>
    public Guid EventId { get; } = Guid.NewGuid();

    /// <inheritdoc/>
    public string EventType => "OrderStatusChanged";
}

/// <summary>
/// Raised when a user requests an OTP for email verification or password reset.
/// Consumed by NotificationService to send the OTP via email.
/// The raw OTP is included here — it is never stored in plain text in the database.
/// </summary>
public record OtpRequestedEvent(
    Guid UserId,
    string Email,
    string FirstName,
    string Otp,
    /// <summary>"email-verification" or "password-reset"</summary>
    string Purpose,
    DateTime OccurredOn) : IIntegrationEvent, SharedKernel.Domain.IDomainEvent
{
    /// <inheritdoc/>
    public Guid EventId { get; } = Guid.NewGuid();

    /// <inheritdoc/>
    public string EventType => "OtpRequested";
}

/// <summary>
/// Represents a single line item within an order, shared across multiple integration events.
/// </summary>
/// <param name="ProductId">The product being ordered.</param>
/// <param name="ProductName">Snapshot of the product name at order time.</param>
/// <param name="Quantity">Number of units ordered.</param>
/// <param name="UnitPrice">Price per unit at the time of order (not current catalogue price).</param>
public record OrderItemDto(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice);

/// <summary>
/// Marker interface for all events that cross service boundaries via the message broker.
/// Distinguishes integration events (inter-service) from domain events (intra-service).
/// </summary>
public interface IIntegrationEvent
{
    /// <summary>UTC timestamp of when the event was raised.</summary>
    DateTime OccurredOn { get; }
}
