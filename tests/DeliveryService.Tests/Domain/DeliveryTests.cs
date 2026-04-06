using DeliveryService.Domain;
using FluentAssertions;
using NUnit.Framework;
using SharedKernel.Events;

namespace DeliveryService.Tests.Domain;

/// <summary>
/// Unit tests for the <see cref="Delivery"/> aggregate root and <see cref="DeliverySlot"/>.
/// </summary>
[TestFixture]
public class DeliveryTests
{
    private static readonly Guid OrderId = Guid.NewGuid();
    private static readonly Guid DriverId = Guid.NewGuid();

    private static Delivery MakeDelivery() =>
        Delivery.Create(OrderId, "123 Main St, Mumbai");

    // ── Create ────────────────────────────────────────────────────────────────

    [Test]
    public void Create_ShouldSetOrderIdAndAddress()
    {
        var d = MakeDelivery();
        d.OrderId.Should().Be(OrderId);
        d.DeliveryAddress.Should().Be("123 Main St, Mumbai");
    }

    [Test]
    public void Create_ShouldDefaultStatusToPending()
    {
        var d = MakeDelivery();
        d.Status.Should().Be(DeliveryStatus.Pending);
    }

    [Test]
    public void Create_ShouldSetScheduledAtToTwoHoursFromNow()
    {
        var before = DateTime.UtcNow.AddHours(2);
        var d = MakeDelivery();
        var after = DateTime.UtcNow.AddHours(2);
        d.ScheduledAt.Should().BeCloseTo(before, TimeSpan.FromSeconds(5));
    }

    [Test]
    public void Create_WithExplicitScheduledAt_ShouldUseProvidedTime()
    {
        var eta = DateTime.UtcNow.AddHours(4);
        var d = Delivery.Create(OrderId, "Addr", eta);
        d.ScheduledAt.Should().Be(eta);
    }

    // ── AssignDriver ──────────────────────────────────────────────────────────

    [Test]
    public void AssignDriver_ShouldSetDriverAndStatus()
    {
        var d = MakeDelivery();
        var eta = DateTime.UtcNow.AddHours(2);
        d.AssignDriver(DriverId, eta);
        d.DriverId.Should().Be(DriverId);
        d.Status.Should().Be(DeliveryStatus.Assigned);
        d.EstimatedDelivery.Should().Be(eta);
    }

    [Test]
    public void AssignDriver_ShouldRaiseDeliveryAssignedEvent()
    {
        var d = MakeDelivery();
        d.AssignDriver(DriverId, DateTime.UtcNow.AddHours(2));
        d.DomainEvents.Should().ContainSingle(e => e is DeliveryAssignedEvent);
    }

    // ── State transitions ─────────────────────────────────────────────────────

    [Test]
    public void PickUp_ShouldSetStatusToPickedUp()
    {
        var d = MakeDelivery();
        d.PickUp();
        d.Status.Should().Be(DeliveryStatus.PickedUp);
    }

    [Test]
    public void StartTransit_ShouldSetStatusToInTransit()
    {
        var d = MakeDelivery();
        d.StartTransit();
        d.Status.Should().Be(DeliveryStatus.InTransit);
    }

    [Test]
    public void Complete_ShouldSetStatusToDeliveredAndSetActualDelivery()
    {
        var d = MakeDelivery();
        d.Complete();
        d.Status.Should().Be(DeliveryStatus.Delivered);
        d.ActualDelivery.Should().NotBeNull();
        d.ActualDelivery!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Test]
    public void Fail_ShouldSetStatusToFailedAndStoreReason()
    {
        var d = MakeDelivery();
        d.Fail("Customer not available");
        d.Status.Should().Be(DeliveryStatus.Failed);
        d.FailureReason.Should().Be("Customer not available");
    }

    // ── UpdateLocation ────────────────────────────────────────────────────────

    [Test]
    public void UpdateLocation_ShouldStoreCoordinates()
    {
        var d = MakeDelivery();
        d.UpdateLocation(19.0760, 72.8777);
        d.CurrentLatitude.Should().Be(19.0760);
        d.CurrentLongitude.Should().Be(72.8777);
    }
}

[TestFixture]
public class DeliverySlotTests
{
    [Test]
    public void Create_ShouldSetTimesAndCapacity()
    {
        var start = DateTime.UtcNow;
        var end = start.AddHours(2);
        var slot = DeliverySlot.Create(start, end, 10);
        slot.StartTime.Should().Be(start);
        slot.EndTime.Should().Be(end);
        slot.MaxCapacity.Should().Be(10);
        slot.CurrentBookings.Should().Be(0);
    }

    [Test]
    public void IsAvailable_WhenUnderCapacity_ShouldBeTrue()
    {
        var slot = DeliverySlot.Create(DateTime.UtcNow, DateTime.UtcNow.AddHours(2), 5);
        slot.IsAvailable.Should().BeTrue();
    }

    [Test]
    public void Book_ShouldIncrementCurrentBookings()
    {
        var slot = DeliverySlot.Create(DateTime.UtcNow, DateTime.UtcNow.AddHours(2), 5);
        slot.Book();
        slot.CurrentBookings.Should().Be(1);
    }

    [Test]
    public void Book_WhenAtCapacity_ShouldNotIncrement()
    {
        var slot = DeliverySlot.Create(DateTime.UtcNow, DateTime.UtcNow.AddHours(2), 1);
        slot.Book(); // fills it
        slot.Book(); // should not increment
        slot.CurrentBookings.Should().Be(1);
    }

    [Test]
    public void IsAvailable_WhenAtCapacity_ShouldBeFalse()
    {
        var slot = DeliverySlot.Create(DateTime.UtcNow, DateTime.UtcNow.AddHours(2), 1);
        slot.Book();
        slot.IsAvailable.Should().BeFalse();
    }
}
