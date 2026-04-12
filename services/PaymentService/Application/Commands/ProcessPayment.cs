using FluentValidation;
using PaymentService.Domain;
using PaymentService.Infrastructure.Services;
using SharedKernel.CQRS;
using SharedKernel.Domain;
using SharedKernel.Messaging;
using SharedKernel.Events;
using DomainPaymentMethod = PaymentService.Domain.PaymentMethod;

namespace PaymentService.Application.Commands;

// ─── Razorpay Payment Flow ────────────────────────────────────────────────────
// Step 1 — ProcessPayment: creates Razorpay order, returns order ID + key to frontend
// Step 2 — VerifyPayment: verifies HMAC signature, marks payment complete
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Command for Step 1 of the Razorpay payment flow.
/// Instructs the system to create a Razorpay order on the Razorpay platform
/// and persist a local payment record in the Pending state.
/// The response carries the Razorpay order ID and public key that the frontend
/// needs to open the Razorpay checkout modal.
/// </summary>
public record ProcessPaymentCommand(
    Guid OrderId,
    Guid CustomerId,
    decimal Amount,
    DomainPaymentMethod Method) : ICommand<ProcessPaymentResponse>;

/// <summary>
/// Response returned after successfully creating a Razorpay order.
/// The frontend uses <c>RazorpayOrderId</c>, <c>AmountPaise</c>, <c>Currency</c>,
/// and <c>KeyId</c> to initialise the Razorpay checkout SDK.
/// <c>PaymentId</c> is the internal record ID used for subsequent operations.
/// </summary>
public record ProcessPaymentResponse(
    Guid PaymentId,
    string RazorpayOrderId,
    long AmountPaise,
    string Currency,
    string KeyId);

/// <summary>
/// FluentValidation rules for <see cref="ProcessPaymentCommand"/>.
/// Ensures the command is structurally valid before any external API calls are made.
/// </summary>
public class ProcessPaymentValidator : AbstractValidator<ProcessPaymentCommand>
{
    public ProcessPaymentValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
    }
}

/// <summary>
/// Handles <see cref="ProcessPaymentCommand"/> by creating a local payment record
/// and then calling the Razorpay API to create a corresponding order.
/// If the Razorpay call fails, the local record is marked as failed and a
/// <see cref="Result{T}.Failure"/> is returned so the caller can surface the error.
/// The two-step approach (persist first, then call Razorpay) ensures we always
/// have a local record even if the external call fails, enabling reconciliation.
/// </summary>
public class ProcessPaymentHandler(
    IPaymentRepository repo,
    IRazorpayPaymentService razorpay,
    IConfiguration config) : ICommandHandler<ProcessPaymentCommand, ProcessPaymentResponse>
{
    /// <summary>
    /// Creates a local <see cref="Payment"/> aggregate in the Pending state,
    /// calls Razorpay to create the order, links the Razorpay order ID to the
    /// local record, and returns the data the frontend needs to open the checkout modal.
    /// </summary>
    public async Task<Result<ProcessPaymentResponse>> Handle(ProcessPaymentCommand cmd, CancellationToken ct)
    {
        var payment = Payment.Create(cmd.OrderId, cmd.CustomerId, cmd.Amount, cmd.Method);
        await repo.AddAsync(payment, ct);

        try
        {
            var rzpOrderId = await razorpay.CreateOrderAsync(cmd.Amount, cmd.OrderId, ct);
            payment.SetRazorpayOrder(rzpOrderId);
            await repo.UpdateAsync(payment, ct);

            return Result<ProcessPaymentResponse>.Success(new ProcessPaymentResponse(
                payment.Id,
                rzpOrderId,
                (long)(cmd.Amount * 100),
                "INR",
                config["Razorpay:KeyId"]!));
        }
        catch (Exception ex)
        {
            payment.Fail(ex.Message);
            await repo.UpdateAsync(payment, ct);
            return Result<ProcessPaymentResponse>.Failure($"Could not create payment order: {ex.Message}");
        }
    }
}

/// <summary>
/// Command for Step 2 of the Razorpay payment flow.
/// Carries the three values returned by the Razorpay frontend SDK after the
/// customer completes payment: the Razorpay order ID, the Razorpay payment ID,
/// and the HMAC-SHA256 signature that proves the callback was not tampered with.
/// </summary>
public record VerifyPaymentCommand(
    string RazorpayOrderId,
    string RazorpayPaymentId,
    string Signature) : ICommand<VerifyPaymentResponse>;

/// <summary>
/// Response returned after successful payment verification.
/// Carries the internal payment ID and the new status string so the caller
/// can update the order record accordingly.
/// </summary>
public record VerifyPaymentResponse(Guid PaymentId, string Status);

/// <summary>
/// Handles <see cref="VerifyPaymentCommand"/> by cryptographically verifying the
/// Razorpay HMAC-SHA256 signature. If valid, the payment is marked as Completed
/// and the Razorpay payment ID is stored for future refund operations.
/// If invalid, the payment is marked as Failed and a <see cref="PaymentFailedEvent"/>
/// is published so downstream services (e.g. order service) can react.
/// </summary>
public class VerifyPaymentHandler(
    IPaymentRepository repo,
    IRazorpayPaymentService razorpay,
    IEventPublisher events) : ICommandHandler<VerifyPaymentCommand, VerifyPaymentResponse>
{
    /// <summary>
    /// Looks up the payment record by Razorpay order ID, verifies the signature,
    /// and transitions the payment to Completed or Failed accordingly.
    /// </summary>
    public async Task<Result<VerifyPaymentResponse>> Handle(VerifyPaymentCommand cmd, CancellationToken ct)
    {
        var payment = await repo.GetByRazorpayOrderIdAsync(cmd.RazorpayOrderId, ct);
        if (payment is null) return Result<VerifyPaymentResponse>.Failure("Payment record not found.");

        if (!razorpay.VerifySignature(cmd.RazorpayOrderId, cmd.RazorpayPaymentId, cmd.Signature))
        {
            payment.Fail("Invalid payment signature.");
            await repo.UpdateAsync(payment, ct);
            await events.PublishAsync(
                new PaymentFailedEvent(payment.Id, payment.OrderId, "Invalid signature", DateTime.UtcNow), ct);
            return Result<VerifyPaymentResponse>.Failure("Payment verification failed. Invalid signature.");
        }

        payment.Complete(cmd.RazorpayPaymentId);
        await repo.UpdateAsync(payment, ct);

        return Result<VerifyPaymentResponse>.Success(
            new VerifyPaymentResponse(payment.Id, payment.Status.ToString()));
    }
}

/// <summary>
/// Command to issue a full refund for a completed payment.
/// Carries the internal payment ID and the reason for the refund.
/// Only completed payments can be refunded; the handler enforces this invariant.
/// </summary>
public record RefundPaymentCommand(Guid PaymentId, string Reason) : ICommand;

/// <summary>
/// Handles <see cref="RefundPaymentCommand"/> by calling the Razorpay refund API
/// and transitioning the local payment record to the Refunded state.
/// Validates that the payment exists and is in the Completed state before
/// attempting the refund to prevent double-refunds.
/// </summary>
public class RefundPaymentHandler(IPaymentRepository repo, IRazorpayPaymentService razorpay)
    : ICommandHandler<RefundPaymentCommand>
{
    /// <inheritdoc/>
    public async Task<Result> Handle(RefundPaymentCommand cmd, CancellationToken ct)
    {
        var payment = await repo.GetByIdAsync(cmd.PaymentId, ct);
        if (payment is null) return Result.Failure("Payment not found.");
        if (payment.Status != PaymentStatus.Completed) return Result.Failure("Only completed payments can be refunded.");

        await razorpay.RefundAsync(payment.RazorpayPaymentId!, ct);
        payment.Refund();
        await repo.UpdateAsync(payment, ct);
        return Result.Success();
    }
}
