using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentService.Application.Commands;
using Stripe;

namespace PaymentService.API.Controllers;

[ApiController]
[Route("api/v1/payments")]
[Authorize]
public class PaymentController(IMediator mediator, IConfiguration config) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> ProcessPayment([FromBody] ProcessPaymentCommand cmd, CancellationToken ct)
    {
        var result = await mediator.Send(cmd, ct);
        if (!result.IsSuccess) return BadRequest(new { result.Error });
        return Ok(result.Value);
    }

    [HttpPost("{id:guid}/refund")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Refund(Guid id, [FromBody] RefundRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(new RefundPaymentCommand(id, req.Reason), ct);
        if (!result.IsSuccess) return BadRequest(new { result.Error });
        return NoContent();
    }

    // Stripe webhook endpoint
    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook(CancellationToken ct)
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync(ct);
        var webhookSecret = config["Stripe:WebhookSecret"]!;

        try
        {
            var stripeEvent = EventUtility.ConstructEvent(
                json, Request.Headers["Stripe-Signature"], webhookSecret);

            await mediator.Send(new HandleStripeWebhookCommand(stripeEvent.Type, json), ct);
            return Ok();
        }
        catch (StripeException)
        {
            return BadRequest();
        }
    }
}

public record RefundRequest(string Reason);
