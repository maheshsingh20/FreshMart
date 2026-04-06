using CartService.Domain;
using FluentAssertions;
using NUnit.Framework;

namespace CartService.Tests.Domain;

/// <summary>
/// Unit tests for the <see cref="Cart"/> aggregate and <see cref="CartItem"/>.
/// Pure domain tests — no mocks, no Redis, no DI.
/// </summary>
[TestFixture]
public class CartTests
{
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid ProductA = Guid.NewGuid();
    private static readonly Guid ProductB = Guid.NewGuid();

    private static Cart MakeCart() => new() { CustomerId = CustomerId };

    // ── AddItem ───────────────────────────────────────────────────────────────

    [Test]
    public void AddItem_NewProduct_ShouldAddToItems()
    {
        var cart = MakeCart();
        cart.AddItem(ProductA, "Milk", 68m, "img.jpg", 2);
        cart.Items.Should().HaveCount(1);
        cart.Items[0].Quantity.Should().Be(2);
    }

    [Test]
    public void AddItem_ExistingProduct_ShouldIncrementQuantity()
    {
        var cart = MakeCart();
        cart.AddItem(ProductA, "Milk", 68m, "img.jpg", 2);
        cart.AddItem(ProductA, "Milk", 68m, "img.jpg", 3);
        cart.Items.Should().HaveCount(1);
        cart.Items[0].Quantity.Should().Be(5);
    }

    [Test]
    public void AddItem_TwoDifferentProducts_ShouldAddBoth()
    {
        var cart = MakeCart();
        cart.AddItem(ProductA, "Milk", 68m, "img.jpg");
        cart.AddItem(ProductB, "Eggs", 90m, "img2.jpg");
        cart.Items.Should().HaveCount(2);
    }

    [Test]
    public void AddItem_WithDiscount_ShouldStoreOriginalPrice()
    {
        var cart = MakeCart();
        cart.AddItem(ProductA, "Milk", 55m, "img.jpg", 1, originalPrice: 68m, discountPercent: 19);
        var item = cart.Items[0];
        item.UnitPrice.Should().Be(55m);
        item.OriginalPrice.Should().Be(68m);
        item.DiscountPercent.Should().Be(19);
    }

    // ── RemoveItem ────────────────────────────────────────────────────────────

    [Test]
    public void RemoveItem_ShouldRemoveFromItems()
    {
        var cart = MakeCart();
        cart.AddItem(ProductA, "Milk", 68m, "img.jpg");
        cart.RemoveItem(ProductA);
        cart.Items.Should().BeEmpty();
    }

    [Test]
    public void RemoveItem_NonExistentProduct_ShouldNotThrow()
    {
        var cart = MakeCart();
        var act = () => cart.RemoveItem(Guid.NewGuid());
        act.Should().NotThrow();
    }

    // ── UpdateQuantity ────────────────────────────────────────────────────────

    [Test]
    public void UpdateQuantity_ShouldChangeQuantity()
    {
        var cart = MakeCart();
        cart.AddItem(ProductA, "Milk", 68m, "img.jpg", 2);
        cart.UpdateQuantity(ProductA, 5);
        cart.Items[0].Quantity.Should().Be(5);
    }

    [Test]
    public void UpdateQuantity_ToZero_ShouldRemoveItem()
    {
        var cart = MakeCart();
        cart.AddItem(ProductA, "Milk", 68m, "img.jpg", 2);
        cart.UpdateQuantity(ProductA, 0);
        cart.Items.Should().BeEmpty();
    }

    [Test]
    public void UpdateQuantity_Negative_ShouldRemoveItem()
    {
        var cart = MakeCart();
        cart.AddItem(ProductA, "Milk", 68m, "img.jpg", 2);
        cart.UpdateQuantity(ProductA, -1);
        cart.Items.Should().BeEmpty();
    }

    // ── SubTotal / TotalItems ─────────────────────────────────────────────────

    [Test]
    public void SubTotal_ShouldSumAllItemTotalPrices()
    {
        var cart = MakeCart();
        cart.AddItem(ProductA, "Milk", 68m, "img.jpg", 2);   // 136
        cart.AddItem(ProductB, "Eggs", 90m, "img2.jpg", 1);  // 90
        cart.SubTotal.Should().Be(226m);
    }

    [Test]
    public void TotalItems_ShouldSumAllQuantities()
    {
        var cart = MakeCart();
        cart.AddItem(ProductA, "Milk", 68m, "img.jpg", 3);
        cart.AddItem(ProductB, "Eggs", 90m, "img2.jpg", 2);
        cart.TotalItems.Should().Be(5);
    }

    // ── Budget ────────────────────────────────────────────────────────────────

    [Test]
    public void IsOverBudget_WhenSubTotalExceedsLimit_ShouldBeTrue()
    {
        var cart = MakeCart();
        cart.SetBudget(100m);
        cart.AddItem(ProductA, "Milk", 68m, "img.jpg", 2); // 136 > 100
        cart.IsOverBudget.Should().BeTrue();
    }

    [Test]
    public void IsOverBudget_WhenSubTotalUnderLimit_ShouldBeFalse()
    {
        var cart = MakeCart();
        cart.SetBudget(500m);
        cart.AddItem(ProductA, "Milk", 68m, "img.jpg", 2); // 136 < 500
        cart.IsOverBudget.Should().BeFalse();
    }

    [Test]
    public void IsOverBudget_WhenNoBudgetSet_ShouldBeFalse()
    {
        var cart = MakeCart();
        cart.AddItem(ProductA, "Milk", 68m, "img.jpg", 100);
        cart.IsOverBudget.Should().BeFalse();
    }

    // ── Clear ─────────────────────────────────────────────────────────────────

    [Test]
    public void Clear_ShouldRemoveAllItems()
    {
        var cart = MakeCart();
        cart.AddItem(ProductA, "Milk", 68m, "img.jpg", 2);
        cart.AddItem(ProductB, "Eggs", 90m, "img2.jpg", 1);
        cart.Clear();
        cart.Items.Should().BeEmpty();
        cart.SubTotal.Should().Be(0);
    }

    // ── CartItem computed ─────────────────────────────────────────────────────

    [Test]
    public void CartItem_TotalPrice_ShouldBeUnitPriceTimesQuantity()
    {
        var cart = MakeCart();
        cart.AddItem(ProductA, "Milk", 68m, "img.jpg", 3);
        cart.Items[0].TotalPrice.Should().Be(204m);
    }

    [Test]
    public void CartItem_OriginalTotalPrice_ShouldUseOriginalPrice()
    {
        var cart = MakeCart();
        cart.AddItem(ProductA, "Milk", 55m, "img.jpg", 2, originalPrice: 68m);
        cart.Items[0].OriginalTotalPrice.Should().Be(136m); // 68 * 2
        cart.Items[0].TotalPrice.Should().Be(110m);         // 55 * 2
    }
}
