using CouponService.Domain;
using CouponService.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CouponService.Application.Commands;

/// <summary>
/// Command to validate a coupon code against a specific order amount.
/// Carries the raw coupon code (case-insensitive) and the order subtotal
/// so the handler can check minimum order requirements and compute the
/// actual discount amount.
/// </summary>
public record ValidateCouponCommand(string Code, decimal OrderAmount);

/// <summary>
/// Result of a coupon validation attempt.
/// <c>Valid</c> indicates whether the coupon can be applied.
/// <c>Message</c> is a human-readable string shown to the customer
/// (e.g. "Coupon applied! You save ₹50.00" or "Minimum order amount is ₹500").
/// <c>DiscountType</c> is either "Percentage" or "Fixed" (null when invalid).
/// <c>DiscountValue</c> is the raw coupon value (e.g. 10 for 10% or ₹10 flat).
/// <c>DiscountAmount</c> is the computed rupee saving for this specific order.
/// </summary>
public record ValidateCouponResult(
    bool Valid,
    string Message,
    string? DiscountType,
    decimal DiscountValue,
    decimal DiscountAmount);

/// <summary>
/// Application service handler for coupon validation.
/// Checks four conditions before approving a coupon:
/// <list type="number">
///   <item>The coupon code exists and is active.</item>
///   <item>The coupon has not expired (<c>ExpiresAt</c> is null or in the future).</item>
///   <item>The coupon has not exceeded its usage limit (<c>UsedCount &lt; UsageLimit</c>).</item>
///   <item>The order amount meets the minimum order requirement.</item>
/// </list>
/// Note: this handler only validates — it does not increment <c>UsedCount</c>.
/// The count should be incremented when the order is confirmed to avoid
/// reserving usage on abandoned checkouts.
/// </summary>
public class ValidateCouponHandler(CouponDbContext db)
{
    /// <summary>
    /// Validates the coupon code against the given order amount and computes
    /// the discount if the coupon is applicable.
    /// For percentage discounts, the discount is capped at the order amount
    /// to prevent negative totals. For fixed discounts, the discount cannot
    /// exceed the order amount either.
    /// </summary>
    /// <param name="cmd">The validation command with code and order amount.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A <see cref="ValidateCouponResult"/> with <c>Valid = true</c> and the
    /// computed discount, or <c>Valid = false</c> with an explanatory message.
    /// </returns>
    public async Task<ValidateCouponResult> HandleAsync(ValidateCouponCommand cmd, CancellationToken ct = default)
    {
        var coupon = await db.Coupons.FirstOrDefaultAsync(c =>
            c.Code == cmd.Code.ToUpper() && c.IsActive &&
            (c.ExpiresAt == null || c.ExpiresAt > DateTime.UtcNow) &&
            c.UsedCount < c.UsageLimit, ct);

        if (coupon is null)
            return new(false, "Invalid or expired coupon code", null, 0, 0);

        if (cmd.OrderAmount < coupon.MinOrderAmount)
            return new(false, $"Minimum order amount is ₹{coupon.MinOrderAmount:F0}", null, 0, 0);

        var discountAmount = coupon.DiscountType == "Percentage"
            ? Math.Round(cmd.OrderAmount * coupon.DiscountValue / 100, 2)
            : Math.Min(coupon.DiscountValue, cmd.OrderAmount);

        return new(true, $"Coupon applied! You save ₹{discountAmount:F2}",
            coupon.DiscountType, coupon.DiscountValue, discountAmount);
    }
}
