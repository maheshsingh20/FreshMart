using CouponService.Application.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CouponService.API.Controllers;

[ApiController]
[Route("api/v1/coupons")]
public class CouponsController(ValidateCouponHandler handler) : ControllerBase
{
    [HttpPost("validate")]
    [Authorize]
    public async Task<IActionResult> Validate([FromBody] CouponValidateRequest req, CancellationToken ct)
    {
        var result = await handler.HandleAsync(new ValidateCouponCommand(req.Code, req.OrderAmount), ct);
        return Ok(result);
    }
}

public record CouponValidateRequest(string Code, decimal OrderAmount);
