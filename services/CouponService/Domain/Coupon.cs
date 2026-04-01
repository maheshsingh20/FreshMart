namespace CouponService.Domain;

/// <summary>
/// Represents a promotional discount coupon that customers can apply at checkout.
/// Supports both percentage-based and fixed-amount discounts with usage limits and expiry.
/// </summary>
public class Coupon
{
    /// <summary>Unique identifier.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Uppercase coupon code entered by the customer (e.g. "WELCOME10").</summary>
    public string Code { get; set; } = "";

    /// <summary>
    /// Discount calculation method.
    /// "Percentage" — deducts a percentage of the order total.
    /// "Fixed" — deducts a fixed rupee amount.
    /// </summary>
    public string DiscountType { get; set; } = "Percentage";

    /// <summary>The discount amount: percentage (0–100) or fixed rupee value depending on <see cref="DiscountType"/>.</summary>
    public decimal DiscountValue { get; set; }

    /// <summary>Minimum order subtotal required for the coupon to be valid.</summary>
    public decimal MinOrderAmount { get; set; }

    /// <summary>Maximum number of times this coupon can be redeemed across all customers.</summary>
    public int UsageLimit { get; set; }

    /// <summary>Number of times this coupon has been successfully applied.</summary>
    public int UsedCount { get; set; }

    /// <summary>Whether the coupon is currently active. Admins can deactivate coupons without deleting them.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Optional UTC expiry date. <c>null</c> means the coupon never expires.</summary>
    public DateTime? ExpiresAt { get; set; }
}
