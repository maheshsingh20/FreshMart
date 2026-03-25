using FluentValidation;
using PaymentService.Domain;
using PaymentService.Infrastructure.Services;
using SharedKernel.CQRS;
using SharedKernel.Domain;
using SharedKernel.Messaging;
using SharedKernel.Events;
using DomainPaymentMethod = PaymentService.Domain.PaymentMethod;

namespace PaymentService.Application.Commands;

// Step 1: Create a Razorpay order and return the order id + key to the frontend
public record ProcessPaymentCommand(
    Guid OrderId,
    Guid CustomerId,
    decimal Amount,
    DomainPaymentMethod Method) : ICommand<ProcessPaymentResponse>;

public record ProcessPaymentResponse(
    Guid PaymentId,
    string RazorpayOrderId,
    long AmountPaise,
    string Currency,
    string KeyId);

public class ProcessPaymentValidator : AbstractValidator<ProcessPaymentCommand>
{
    public ProcessPaymentValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
    }
}

public class ProcessPaymentHandler(
    IPaymentRepository repo,
    IRazorpayPaymentService razorpay,
    IConfiguration config) : ICommandHandler<ProcessPaymentCommand, ProcessPaymentResponse>
{
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

// Step 2: Verify the signature after frontend completes payment
public record VerifyPaymentCommand(
    string RazorpayOrderId,
    string RazorpayPaymentId,
    string Signature) : ICommand<VerifyPaymentResponse>;

public record VerifyPaymentResponse(Guid PaymentId, string Status);

public class VerifyPaymentHandler(
    IPaymentRepository repo,
    IRazorpayPaymentService razorpay,
    IEventPublisher events) : ICommandHandler<VerifyPaymentCommand, VerifyPaymentResponse>
{
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

// Refund
public record RefundPaymentCommand(Guid PaymentId, string Reason) : ICommand;

public class RefundPaymentHandler(IPaymentRepository repo, IRazorpayPaymentService razorpay)
    : ICommandHandler<RefundPaymentCommand>
{
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
