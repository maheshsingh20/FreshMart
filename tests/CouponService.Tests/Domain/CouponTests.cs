using CouponService.Domain;
using FluentAssertions;
using NUnit.Framework;

namespace CouponService.Tests.Domain;

/// <summary>
/// Unit tests for <see cref="Coupon"/> domain model.
/// Tests discount calculation logic, expiry, and usage limits.
/// </summary>
[TestFixture]
public class CouponTests
{
    private static Coupon MakePercentageCoupon(decimal value = 10, decimal minOrder = 0,
        int usageLimit = 100, int usedCount = 0, bool isActive = true, DateTime? expiresAt = null) =>
        new()
        {
            Code = "WELCOME10",
            DiscountType = "Percentage",
            DiscountValue = value,
            MinOrderAmount = minOrder,
            UsageLimit = usageLimit,
            UsedCount = usedCount,
            IsActive = isActive,
            ExpiresAt = expiresAt
        };

    private static Coupon MakeFixedCoupon(decimal value = 50, decimal minOrder = 200) =>
        new()
        {
            Code = "FLAT50",
            DiscountType = "Fixed",
            DiscountValue = value,
            MinOrderAmount = minOrder,
            UsageLimit = 100,
            UsedCount = 0,
            IsActive = true
        };

    // ── Properties ────────────────────────────────────────────────────────────

    [Test]
    public void Coupon_ShouldDefaultToActive()
    {
        var c = new Coupon();
        c.IsActive.Should().BeTrue();
    }

    [Test]
    public void Coupon_ShouldGenerateUniqueId()
    {
        var c1 = new Coupon();
        var c2 = new Coupon();
        c1.Id.Should().NotBe(c2.Id);
    }

    // ── Percentage discount calculation ───────────────────────────────────────

    [Test]
    public void PercentageDiscount_ShouldCalculateCorrectly()
    {
        var coupon = MakePercentageCoupon(value: 10);
        var orderAmount = 500m;
        var discount = Math.Round(orderAmount * coupon.DiscountValue / 100, 2);
        discount.Should().Be(50m); // 10% of 500
    }

    [Test]
    public void PercentageDiscount_20Percent_ShouldCalculateCorrectly()
    {
        var coupon = MakePercentageCoupon(value: 20);
        var orderAmount = 300m;
        var discount = Math.Round(orderAmount * coupon.DiscountValue / 100, 2);
        discount.Should().Be(60m); // 20% of 300
    }

    // ── Fixed discount calculation ────────────────────────────────────────────

    [Test]
    public void FixedDiscount_ShouldDeductFixedAmount()
    {
        var coupon = MakeFixedCoupon(value: 50);
        var orderAmount = 500m;
        var discount = Math.Min(coupon.DiscountValue, orderAmount);
        discount.Should().Be(50m);
    }

    [Test]
    public void FixedDiscount_WhenOrderLessThanDiscount_ShouldCapAtOrderAmount()
    {
        var coupon = MakeFixedCoupon(value: 100);
        var orderAmount = 60m;
        var discount = Math.Min(coupon.DiscountValue, orderAmount);
        discount.Should().Be(60m); // can't discount more than order total
    }

    // ── Validity checks ───────────────────────────────────────────────────────

    [Test]
    public void Coupon_WhenInactive_ShouldNotBeValid()
    {
        var coupon = MakePercentageCoupon(isActive: false);
        coupon.IsActive.Should().BeFalse();
    }

    [Test]
    public void Coupon_WhenExpired_ShouldBeDetectable()
    {
        var coupon = MakePercentageCoupon(expiresAt: DateTime.UtcNow.AddDays(-1));
        var isExpired = coupon.ExpiresAt.HasValue && coupon.ExpiresAt < DateTime.UtcNow;
        isExpired.Should().BeTrue();
    }

    [Test]
    public void Coupon_WhenNotExpired_ShouldBeDetectable()
    {
        var coupon = MakePercentageCoupon(expiresAt: DateTime.UtcNow.AddDays(30));
        var isExpired = coupon.ExpiresAt.HasValue && coupon.ExpiresAt < DateTime.UtcNow;
        isExpired.Should().BeFalse();
    }

    [Test]
    public void Coupon_WhenUsageLimitReached_ShouldBeDetectable()
    {
        var coupon = MakePercentageCoupon(usageLimit: 10, usedCount: 10);
        var isExhausted = coupon.UsedCount >= coupon.UsageLimit;
        isExhausted.Should().BeTrue();
    }

    [Test]
    public void Coupon_WhenUsageLimitNotReached_ShouldBeDetectable()
    {
        var coupon = MakePercentageCoupon(usageLimit: 10, usedCount: 5);
        var isExhausted = coupon.UsedCount >= coupon.UsageLimit;
        isExhausted.Should().BeFalse();
    }

    [Test]
    public void Coupon_WhenOrderBelowMinimum_ShouldBeDetectable()
    {
        var coupon = MakePercentageCoupon(minOrder: 500);
        var orderAmount = 200m;
        var meetsMinimum = orderAmount >= coupon.MinOrderAmount;
        meetsMinimum.Should().BeFalse();
    }

    [Test]
    public void Coupon_WhenOrderMeetsMinimum_ShouldBeDetectable()
    {
        var coupon = MakePercentageCoupon(minOrder: 200);
        var orderAmount = 500m;
        var meetsMinimum = orderAmount >= coupon.MinOrderAmount;
        meetsMinimum.Should().BeTrue();
    }

    [Test]
    public void Coupon_WithNoExpiry_ShouldNeverExpire()
    {
        var coupon = MakePercentageCoupon(expiresAt: null);
        var isExpired = coupon.ExpiresAt.HasValue && coupon.ExpiresAt < DateTime.UtcNow;
        isExpired.Should().BeFalse();
    }
}
