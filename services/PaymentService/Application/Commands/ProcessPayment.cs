using FluentValidation;
using PaymentService.Domain;
using SharedKernel.CQRS;
using SharedKernel.Domain;
using SharedKernel.Messaging;
using SharedKernel.Events;
using Stripe;

namespace PaymentService.Application.Commands;

public record ProcessPaymentCommand(
    Guid OrderId,
    Guid CustomerId,
    decimal Amount,
    string PaymentMethodId,
    PaymentMethod Method) : ICommand<ProcessPaymentResponse>;

public record ProcessPaymentResponse(Guid PaymentId, string Status, string? ClientSecret);

public record RefundPaymentCommand(Guid PaymentId, string Reason) : ICommand;

public class ProcessPaymentValidator : AbstractValidator<ProcessPaymentCommand>
{
    public ProcessPaymentValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.PaymentMethodId).NotEmpty();
    }
}

public class ProcessPaymentHandler(
    IPaymentRepository repo,
    IEventPublisher events,
    IStripePaymentService stripe) : ICommandHandler<ProcessPaymentCommand, ProcessPaymentResponse>
{
    public async Task<Result<ProcessPaymentResponse>> Handle(ProcessPaymentCommand cmd, CancellationToken ct)
    {
        var payment = Payment.Create(cmd.OrderId, cmd.CustomerId, cmd.Amount, cmd.Method);
        await repo.AddAsync(payment, ct);

        try
        {
            var (intentId, clientSecret) = await stripe.CreatePaymentIntentAsync(
                cmd.Amount, cmd.PaymentMethodId, cmd.OrderId.ToString(), ct);

            payment.SetStripeIntent(intentId);
            await repo.UpdateAsync(payment, ct);

            return Result<ProcessPaymentResponse>.Success(
                new ProcessPaymentResponse(payment.Id, payment.Status.ToString(), clientSecret));
        }
        catch (StripeException ex)
        {
            payment.Fail(ex.Message);
            await repo.UpdateAsync(payment, ct);

            await events.PublishAsync(
                new PaymentFailedEvent(payment.Id, payment.OrderId, ex.Message, DateTime.UtcNow), ct);

            return Result<ProcessPaymentResponse>.Failure($"Payment failed: {ex.Message}");
        }
    }
}

public class RefundPaymentHandler(IPaymentRepository repo, IStripePaymentService stripe)
    : ICommandHandler<RefundPaymentCommand>
{
    public async Task<Result> Handle(RefundPaymentCommand cmd, CancellationToken ct)
    {
        var payment = await repo.GetByIdAsync(cmd.PaymentId, ct);
        if (payment is null) return Result.Failure("Payment not found.");
        if (payment.Status != PaymentStatus.Completed) return Result.Failure("Only completed payments can be refunded.");

        await stripe.RefundAsync(payment.StripeChargeId!, cmd.Reason, ct);
        payment.Refund();
        await repo.UpdateAsync(payment, ct);
        return Result.Success();
    }
}
