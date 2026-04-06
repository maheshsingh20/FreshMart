using FluentAssertions;
using NUnit.Framework;
using ProductService.Domain;

namespace ProductService.Tests.Domain;

/// <summary>
/// Unit tests for the <see cref="Product"/> aggregate root.
/// Pure domain tests — no mocks, no database, no DI.
/// </summary>
[TestFixture]
public class ProductTests
{
    private static Product MakeProduct(int stock = 50) =>
        Product.Create("Amul Milk", "Full cream 1L", 68m, "DE001",
            "https://img.com/milk.jpg", Guid.NewGuid(), stock, "Amul", null, "1L");

    // ── Create ────────────────────────────────────────────────────────────────

    [Test]
    public void Create_ShouldSetPropertiesCorrectly()
    {
        var p = MakeProduct();
        p.Name.Should().Be("Amul Milk");
        p.Price.Should().Be(68m);
        p.SKU.Should().Be("DE001");
        p.StockQuantity.Should().Be(50);
        p.IsActive.Should().BeTrue();
        p.Brand.Should().Be("Amul");
        p.Unit.Should().Be("1L");
    }

    [Test]
    public void Create_ShouldRaiseProductCreatedEvent()
    {
        var p = MakeProduct();
        p.DomainEvents.Should().ContainSingle(e => e is ProductCreatedEvent);
    }

    [Test]
    public void Create_ShouldDefaultDiscountToZero()
    {
        var p = MakeProduct();
        p.DiscountPercent.Should().Be(0);
    }

    // ── Stock ─────────────────────────────────────────────────────────────────

    [Test]
    public void UpdateStock_ShouldChangeStockQuantity()
    {
        var p = MakeProduct(50);
        p.UpdateStock(30);
        p.StockQuantity.Should().Be(30);
    }

    [Test]
    public void UpdateStock_ShouldRaiseInventoryUpdatedEvent()
    {
        var p = MakeProduct(50);
        p.ClearDomainEvents();
        p.UpdateStock(30);
        p.DomainEvents.Should().ContainSingle(e => e is InventoryUpdatedDomainEvent);
    }

    [Test]
    public void UpdateStock_BelowThreshold_ShouldRaiseLowStockEvent()
    {
        var p = MakeProduct(50);
        p.ClearDomainEvents();
        p.UpdateStock(5); // below default threshold of 10
        p.DomainEvents.Should().Contain(e => e is LowStockDomainEvent);
    }

    [Test]
    public void DeductStock_ShouldReduceStockByAmount()
    {
        var p = MakeProduct(50);
        p.DeductStock(10);
        p.StockQuantity.Should().Be(40);
    }

    [Test]
    public void DeductStock_InsufficientStock_ShouldThrow()
    {
        var p = MakeProduct(5);
        var act = () => p.DeductStock(10);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Insufficient stock*");
    }

    [Test]
    public void DeductStock_ExactAmount_ShouldSetStockToZero()
    {
        var p = MakeProduct(10);
        p.DeductStock(10);
        p.StockQuantity.Should().Be(0);
    }

    // ── Discount ──────────────────────────────────────────────────────────────

    [Test]
    public void SetDiscount_ShouldUpdateDiscountPercent()
    {
        var p = MakeProduct();
        p.SetDiscount(20);
        p.DiscountPercent.Should().Be(20);
    }

    [Test]
    public void SetDiscount_Above100_ShouldClampTo100()
    {
        var p = MakeProduct();
        p.SetDiscount(150);
        p.DiscountPercent.Should().Be(100);
    }

    [Test]
    public void SetDiscount_Negative_ShouldClampToZero()
    {
        var p = MakeProduct();
        p.SetDiscount(-10);
        p.DiscountPercent.Should().Be(0);
    }

    // ── Active / Inactive ─────────────────────────────────────────────────────

    [Test]
    public void Deactivate_ShouldSetIsActiveFalse()
    {
        var p = MakeProduct();
        p.Deactivate();
        p.IsActive.Should().BeFalse();
    }

    [Test]
    public void Activate_ShouldSetIsActiveTrue()
    {
        var p = MakeProduct();
        p.Deactivate();
        p.Activate();
        p.IsActive.Should().BeTrue();
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Test]
    public void Update_ShouldChangeNameAndPrice()
    {
        var p = MakeProduct();
        p.Update("Amul Gold", "Full cream 500ml", 45m, "https://img.com/gold.jpg",
            p.CategoryId, "Amul", "500ml", null, true);
        p.Name.Should().Be("Amul Gold");
        p.Price.Should().Be(45m);
    }
}
