using SharedKernel.Domain;
using SharedKernel.Events;

namespace PaymentService.Domain;

public enum PaymentStatus { Pending, Processing, Completed, Failed, Refunded }
public enum PaymentMethod { CreditCard, DebitCard, DigitalWallet }

public class Payment : AggregateRoot
{
    public Guid OrderId { get; private set; }
    public Guid CustomerId { get; private set; }
    public decimal Amount { get; private set; }
    public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;
    public PaymentMethod Method { get; private set; }
    public string? StripePaymentIntentId { get; private set; }
    public string? StripeChargeId { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime? ProcessedAt { get; private set; }

    private Payment() { }

    public static Payment Create(Guid orderId, Guid customerId, decimal amount, PaymentMethod method) =>
        new() { OrderId = orderId, CustomerId = customerId, Amount = amount, Method = method };

    public void SetStripeIntent(string intentId)
    {
        StripePaymentIntentId = intentId;
        Status = PaymentStatus.Processing;
        SetUpdated();
    }

    public void Complete(string chargeId)
    {
        Status = PaymentStatus.Completed;
        StripeChargeId = chargeId;
        ProcessedAt = DateTime.UtcNow;
        SetUpdated();
        AddDomainEvent(new PaymentCompletedEvent(Id, OrderId, Amount, chargeId, DateTime.UtcNow));
    }

    public void Fail(string reason)
    {
        Status = PaymentStatus.Failed;
        FailureReason = reason;
        SetUpdated();
        AddDomainEvent(new PaymentFailedEvent(Id, OrderId, reason, DateTime.UtcNow));
    }

    public void Refund()
    {
        Status = PaymentStatus.Refunded;
        SetUpdated();
    }
}
