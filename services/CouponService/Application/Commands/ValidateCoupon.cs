using CouponService.Domain;
using CouponService.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CouponService.Application.Commands;

public record ValidateCouponCommand(string Code, decimal OrderAmount);

public record ValidateCouponResult(
    bool Valid,
    string Message,
    string? DiscountType,
    decimal DiscountValue,
    decimal DiscountAmount);

public class ValidateCouponHandler(CouponDbContext db)
{
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
