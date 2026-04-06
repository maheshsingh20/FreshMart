using FluentAssertions;
using NUnit.Framework;
using PaymentService.Domain;
using SharedKernel.Events;

namespace PaymentService.Tests.Domain;

/// <summary>
/// Unit tests for the <see cref="Payment"/> aggregate root.
/// Covers creation, Razorpay order linking, completion, failure, and refund.
/// </summary>
[TestFixture]
public class PaymentTests
{
    private static readonly Guid OrderId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();

    private static Payment MakePayment(decimal amount = 500m) =>
        Payment.Create(OrderId, CustomerId, amount, PaymentMethod.UPI);

    // ── Create ────────────────────────────────────────────────────────────────

    [Test]
    public void Create_ShouldSetPropertiesCorrectly()
    {
        var p = MakePayment(500m);
        p.OrderId.Should().Be(OrderId);
        p.CustomerId.Should().Be(CustomerId);
        p.Amount.Should().Be(500m);
        p.Method.Should().Be(PaymentMethod.UPI);
    }

    [Test]
    public void Create_ShouldDefaultStatusToPending()
    {
        var p = MakePayment();
        p.Status.Should().Be(PaymentStatus.Pending);
    }

    [Test]
    public void Create_ShouldHaveNoRazorpayIds()
    {
        var p = MakePayment();
        p.RazorpayOrderId.Should().BeNull();
        p.RazorpayPaymentId.Should().BeNull();
    }

    // ── SetRazorpayOrder ──────────────────────────────────────────────────────

    [Test]
    public void SetRazorpayOrder_ShouldStoreIdAndSetProcessing()
    {
        var p = MakePayment();
        p.SetRazorpayOrder("order_abc123");
        p.RazorpayOrderId.Should().Be("order_abc123");
        p.Status.Should().Be(PaymentStatus.Processing);
    }

    // ── Complete ──────────────────────────────────────────────────────────────

    [Test]
    public void Complete_ShouldSetStatusToCompleted()
    {
        var p = MakePayment();
        p.SetRazorpayOrder("order_abc");
        p.Complete("pay_xyz");
        p.Status.Should().Be(PaymentStatus.Completed);
    }

    [Test]
    public void Complete_ShouldStoreRazorpayPaymentId()
    {
        var p = MakePayment();
        p.Complete("pay_xyz");
        p.RazorpayPaymentId.Should().Be("pay_xyz");
    }

    [Test]
    public void Complete_ShouldSetProcessedAt()
    {
        var p = MakePayment();
        p.Complete("pay_xyz");
        p.ProcessedAt.Should().NotBeNull();
        p.ProcessedAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Test]
    public void Complete_ShouldRaisePaymentCompletedEvent()
    {
        var p = MakePayment();
        p.Complete("pay_xyz");
        p.DomainEvents.Should().ContainSingle(e => e is PaymentCompletedEvent);
    }

    // ── Fail ──────────────────────────────────────────────────────────────────

    [Test]
    public void Fail_ShouldSetStatusToFailed()
    {
        var p = MakePayment();
        p.Fail("Insufficient funds");
        p.Status.Should().Be(PaymentStatus.Failed);
    }

    [Test]
    public void Fail_ShouldStoreFailureReason()
    {
        var p = MakePayment();
        p.Fail("Card declined");
        p.FailureReason.Should().Be("Card declined");
    }

    [Test]
    public void Fail_ShouldRaisePaymentFailedEvent()
    {
        var p = MakePayment();
        p.Fail("Network error");
        p.DomainEvents.Should().ContainSingle(e => e is PaymentFailedEvent);
    }

    // ── Refund ────────────────────────────────────────────────────────────────

    [Test]
    public void Refund_ShouldSetStatusToRefunded()
    {
        var p = MakePayment();
        p.Complete("pay_xyz");
        p.Refund();
        p.Status.Should().Be(PaymentStatus.Refunded);
    }
}
