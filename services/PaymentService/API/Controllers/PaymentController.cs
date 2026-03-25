using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentService.Application.Commands;

namespace PaymentService.API.Controllers;

[ApiController]
[Route("api/v1/payments")]
[Authorize]
public class PaymentController(IMediator mediator) : ControllerBase
{
    // Step 1: Create Razorpay order — returns razorpayOrderId + keyId to frontend
    [HttpPost]
    public async Task<IActionResult> CreatePayment([FromBody] ProcessPaymentCommand cmd, CancellationToken ct)
    {
        var result = await mediator.Send(cmd, ct);
        if (!result.IsSuccess) return BadRequest(new { result.Error });
        return Ok(result.Value);
    }

    // Step 2: Verify signature after frontend completes payment via Razorpay SDK
    [HttpPost("verify")]
    public async Task<IActionResult> VerifyPayment([FromBody] VerifyPaymentCommand cmd, CancellationToken ct)
    {
        var result = await mediator.Send(cmd, ct);
        if (!result.IsSuccess) return BadRequest(new { result.Error });
        return Ok(result.Value);
    }

    // Refund — Admin only
    [HttpPost("{id:guid}/refund")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Refund(Guid id, [FromBody] RefundRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(new RefundPaymentCommand(id, req.Reason), ct);
        if (!result.IsSuccess) return BadRequest(new { result.Error });
        return NoContent();
    }

    // Razorpay webhook — no auth, signature verified inside handler
    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook(CancellationToken ct)
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync(ct);
        var signature = Request.Headers["X-Razorpay-Signature"].ToString();

        var result = await mediator.Send(new HandleRazorpayWebhookCommand(json, signature), ct);
        if (!result.IsSuccess) return BadRequest();
        return Ok();
    }
}

public record RefundRequest(string Reason);
