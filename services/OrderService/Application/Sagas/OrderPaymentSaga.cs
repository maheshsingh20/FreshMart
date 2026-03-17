using MediatR;
using OrderService.Application.Commands;
using SharedKernel.Events;

namespace OrderService.Application.Sagas;

/// <summary>
/// Saga orchestrator for the Order-Payment distributed transaction.
/// Listens to PaymentCompleted/PaymentFailed events and drives order state.
/// Implements compensation (rollback) on failure.
/// </summary>
public class OrderPaymentSaga(IMediator mediator, ILogger<OrderPaymentSaga> logger)
    : INotificationHandler<PaymentCompletedIntegrationEvent>,
      INotificationHandler<PaymentFailedIntegrationEvent>
{
    // Step 2: Payment succeeded → confirm order
    public async Task Handle(PaymentCompletedIntegrationEvent notification, CancellationToken ct)
    {
        logger.LogInformation("Saga: Payment {PaymentId} completed for Order {OrderId}",
            notification.PaymentId, notification.OrderId);

        var result = await mediator.Send(new ConfirmPaymentCommand(notification.OrderId), ct);
        if (!result.IsSuccess)
            logger.LogError("Saga: Failed to confirm order {OrderId}: {Error}", notification.OrderId, result.Error);
    }

    // Compensation: Payment failed → fail order (triggers inventory restore)
    public async Task Handle(PaymentFailedIntegrationEvent notification, CancellationToken ct)
    {
        logger.LogWarning("Saga: Payment failed for Order {OrderId}. Reason: {Reason}",
            notification.OrderId, notification.Reason);

        var result = await mediator.Send(new FailPaymentCommand(notification.OrderId), ct);
        if (!result.IsSuccess)
            logger.LogError("Saga: Failed to update order status for {OrderId}", notification.OrderId);
    }
}

// MediatR notification wrappers for integration events
public record PaymentCompletedIntegrationEvent(Guid PaymentId, Guid OrderId, decimal Amount, string TransactionId)
    : INotification;

public record PaymentFailedIntegrationEvent(Guid PaymentId, Guid OrderId, string Reason)
    : INotification;
