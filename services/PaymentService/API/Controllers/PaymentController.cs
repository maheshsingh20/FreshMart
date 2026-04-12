using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentService.Application.Commands;

namespace PaymentService.API.Controllers;

/// <summary>
/// HTTP API controller for the Razorpay payment lifecycle.
/// Exposes three authenticated endpoints for the standard online payment flow
/// (create order → verify signature → optional refund) plus an unauthenticated
/// webhook endpoint for Razorpay server-to-server event delivery.
/// All business logic is delegated to MediatR command handlers, keeping the
/// controller thin and focused on HTTP concerns only.
/// </summary>
[ApiController]
[Route("api/v1/payments")]
[Authorize]
public class PaymentController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Step 1 of the Razorpay checkout flow.
    /// Creates a Razorpay order on the Razorpay platform and returns the
    /// <c>razorpayOrderId</c>, amount in paise, currency, and the public API key
    /// to the frontend. The frontend uses these values to initialise the
    /// Razorpay checkout modal/SDK.
    /// </summary>
    /// <param name="cmd">
    /// Contains the internal order ID, customer ID, amount in INR, and payment method.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// 200 OK with a <see cref="ProcessPaymentResponse"/>, or 400 Bad Request
    /// if the Razorpay API call fails.
    /// </returns>
    [HttpPost]
    public async Task<IActionResult> CreatePayment([FromBody] ProcessPaymentCommand cmd, CancellationToken ct)
    {
        var result = await mediator.Send(cmd, ct);
        if (!result.IsSuccess) return BadRequest(new { result.Error });
        return Ok(result.Value);
    }

    /// <summary>
    /// Step 2 of the Razorpay checkout flow.
    /// After the customer completes payment in the Razorpay modal, the frontend
    /// receives a callback with the Razorpay order ID, payment ID, and HMAC-SHA256
    /// signature. This endpoint verifies the signature cryptographically to confirm
    /// the payment was not tampered with, then marks the payment record as completed.
    /// </summary>
    /// <param name="cmd">The three Razorpay callback values needed for signature verification.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// 200 OK with a <see cref="VerifyPaymentResponse"/> on success, or 400 Bad Request
    /// if the signature is invalid.
    /// </returns>
    [HttpPost("verify")]
    public async Task<IActionResult> VerifyPayment([FromBody] VerifyPaymentCommand cmd, CancellationToken ct)
    {
        var result = await mediator.Send(cmd, ct);
        if (!result.IsSuccess) return BadRequest(new { result.Error });
        return Ok(result.Value);
    }

    /// <summary>
    /// Issues a full refund for a completed payment.
    /// Restricted to the Admin role to prevent customers from self-refunding.
    /// Delegates to Razorpay's refund API via the command handler.
    /// </summary>
    /// <param name="id">The internal payment record ID to refund.</param>
    /// <param name="req">The reason for the refund (stored for audit purposes).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>204 No Content on success, or 400 Bad Request if the payment cannot be refunded.</returns>
    [HttpPost("{id:guid}/refund")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Refund(Guid id, [FromBody] RefundRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(new RefundPaymentCommand(id, req.Reason), ct);
        if (!result.IsSuccess) return BadRequest(new { result.Error });
        return NoContent();
    }

    /// <summary>
    /// Receives server-to-server webhook events from Razorpay (e.g. payment.captured,
    /// payment.failed). This endpoint is intentionally unauthenticated because Razorpay
    /// calls it directly — security is enforced by verifying the
    /// <c>X-Razorpay-Signature</c> header inside the command handler using the
    /// shared webhook secret. Returning 200 OK acknowledges receipt to Razorpay.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>200 OK if the webhook is valid and processed; 400 Bad Request if the signature fails.</returns>
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

/// <summary>
/// Request body for the refund endpoint, capturing the reason for audit and
/// potential customer communication.
/// </summary>
public record RefundRequest(string Reason);
