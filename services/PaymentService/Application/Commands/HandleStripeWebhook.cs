using PaymentService.Domain;
using SharedKernel.CQRS;
using SharedKernel.Domain;
using SharedKernel.Events;
using SharedKernel.Messaging;

namespace PaymentService.Application.Commands;

public record HandleStripeWebhookCommand(string EventType, string Payload) : ICommand;

public class HandleStripeWebhookHandler(IPaymentRepository repo, IEventPublisher events)
    : ICommandHandler<HandleStripeWebhookCommand>
{
    public async Task<Result> Handle(HandleStripeWebhookCommand cmd, CancellationToken ct)
    {
        // Parse Stripe event and update payment accordingly
        switch (cmd.EventType)
        {
            case "payment_intent.succeeded":
                // Extract payment intent ID from payload and mark payment complete
                break;
            case "payment_intent.payment_failed":
                // Mark payment as failed and publish compensation event
                break;
            case "charge.refunded":
                // Mark payment as refunded
                break;
        }
        return Result.Success();
    }
}
