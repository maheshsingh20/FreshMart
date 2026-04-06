using FluentAssertions;
using NUnit.Framework;
using OrderService.Domain;
using SharedKernel.Events;
using System.Linq;

namespace OrderService.Tests.Domain;

/// <summary>
/// Unit tests for the <see cref="Order"/> aggregate root.
/// Covers order creation, pricing, state machine transitions, and cancellation rules.
/// </summary>
[TestFixture]
public class OrderTests
{
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid ProductId = Guid.NewGuid();

    private static Order MakeOrder(decimal price = 100m, int qty = 2) =>
        Order.Create(CustomerId, "123 Main St, Mumbai",
            [(ProductId, "Amul Milk", qty, price)],
            deliveryFee: 49m, taxRate: 0.05m,
            customerEmail: "test@example.com", customerFirstName: "John");

    // ── Create ────────────────────────────────────────────────────────────────

    [Test]
    public void Create_ShouldSetCustomerAndAddress()
    {
        var order = MakeOrder();
        order.CustomerId.Should().Be(CustomerId);
        order.DeliveryAddress.Should().Be("123 Main St, Mumbai");
        order.CustomerEmail.Should().Be("test@example.com");
        order.CustomerFirstName.Should().Be("John");
    }

    [Test]
    public void Create_ShouldCalculateSubTotalCorrectly()
    {
        var order = MakeOrder(price: 100m, qty: 2);
        order.SubTotal.Should().Be(200m); // 100 * 2
    }

    [Test]
    public void Create_ShouldCalculateTaxCorrectly()
    {
        var order = MakeOrder(price: 100m, qty: 2);
        order.TaxAmount.Should().Be(10m); // 200 * 5%
    }

    [Test]
    public void Create_ShouldCalculateTotalCorrectly()
    {
        var order = MakeOrder(price: 100m, qty: 2);
        order.TotalAmount.Should().Be(259m); // 200 + 49 + 10
    }

    [Test]
    public void Create_ShouldDefaultStatusToPending()
    {
        var order = MakeOrder();
        order.Status.Should().Be(OrderStatus.Pending);
    }

    [Test]
    public void Create_ShouldRaiseOrderCreatedEvent()
    {
        var order = MakeOrder();
        order.DomainEvents.Should().ContainSingle(e => e is OrderCreatedEvent);
    }

    [Test]
    public void Create_ShouldAddItemsCorrectly()
    {
        var order = MakeOrder(qty: 3);
        order.Items.Should().HaveCount(1);
        order.Items.First().Quantity.Should().Be(3);
        order.Items.First().ProductName.Should().Be("Amul Milk");
    }

    // ── State machine ─────────────────────────────────────────────────────────

    [Test]
    public void ConfirmPayment_ShouldSetStatusToPaymentConfirmed()
    {
        var order = MakeOrder();
        order.ConfirmPayment();
        order.Status.Should().Be(OrderStatus.PaymentConfirmed);
    }

    [Test]
    public void FailPayment_ShouldSetStatusToPaymentFailed()
    {
        var order = MakeOrder();
        order.FailPayment();
        order.Status.Should().Be(OrderStatus.PaymentFailed);
    }

    [Test]
    public void StartProcessing_ShouldSetStatusToProcessing()
    {
        var order = MakeOrder();
        order.ConfirmPayment();
        order.StartProcessing();
        order.Status.Should().Be(OrderStatus.Processing);
    }

    [Test]
    public void Ship_ShouldSetStatusToShipped()
    {
        var order = MakeOrder();
        order.Ship();
        order.Status.Should().Be(OrderStatus.Shipped);
    }

    [Test]
    public void Deliver_ShouldSetStatusToDeliveredAndSetDeliveredAt()
    {
        var order = MakeOrder();
        order.Deliver();
        order.Status.Should().Be(OrderStatus.Delivered);
        order.DeliveredAt.Should().NotBeNull();
        order.DeliveredAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    // ── Cancel ────────────────────────────────────────────────────────────────

    [Test]
    public void Cancel_PendingOrder_ShouldSetStatusToCancelled()
    {
        var order = MakeOrder();
        order.Cancel("Customer request");
        order.Status.Should().Be(OrderStatus.Cancelled);
        order.CancellationReason.Should().Be("Customer request");
    }

    [Test]
    public void Cancel_DeliveredOrder_ShouldThrow()
    {
        var order = MakeOrder();
        order.Deliver();
        var act = () => order.Cancel("Too late");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot cancel*");
    }

    [Test]
    public void Cancel_ShippedOrder_ShouldThrow()
    {
        var order = MakeOrder();
        order.Ship();
        var act = () => order.Cancel("Changed mind");
        act.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void Cancel_ShouldRaiseOrderCancelledEvent()
    {
        var order = MakeOrder();
        order.ClearDomainEvents();
        order.Cancel("Test");
        order.DomainEvents.Should().ContainSingle(e => e is OrderCancelledEvent);
    }

    // ── EstimatedDelivery ─────────────────────────────────────────────────────

    [Test]
    public void SetEstimatedDelivery_ShouldUpdateEta()
    {
        var order = MakeOrder();
        var eta = DateTime.UtcNow.AddDays(2);
        order.SetEstimatedDelivery(eta);
        order.EstimatedDelivery.Should().Be(eta);
    }
}
