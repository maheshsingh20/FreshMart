using Backend.Data;
using Backend.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/v1/coupons")]
public class CouponsController(AppDbContext db) : ControllerBase
{
    [HttpPost("validate")]
    [Authorize]
    public async Task<IActionResult> Validate(CouponValidateRequest req)
    {
        var coupon = await db.Coupons.FirstOrDefaultAsync(c =>
            c.Code == req.Code.ToUpper() && c.IsActive &&
            (c.ExpiresAt == null || c.ExpiresAt > DateTime.UtcNow) &&
            c.UsedCount < c.UsageLimit);

        if (coupon == null)
            return Ok(new CouponValidateResponse(false, "Invalid or expired coupon code", null, 0, 0));

        if (req.OrderAmount < coupon.MinOrderAmount)
            return Ok(new CouponValidateResponse(false, $"Minimum order amount is Rs.{coupon.MinOrderAmount:F0}", null, 0, 0));

        var discountAmount = coupon.DiscountType == "Percentage"
            ? Math.Round(req.OrderAmount * coupon.DiscountValue / 100, 2)
            : Math.Min(coupon.DiscountValue, req.OrderAmount);

        return Ok(new CouponValidateResponse(true, $"Coupon applied! You save Rs.{discountAmount:F2}",
            coupon.DiscountType, coupon.DiscountValue, discountAmount));
    }
}
