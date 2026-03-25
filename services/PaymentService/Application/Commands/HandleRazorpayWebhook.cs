using PaymentService.Domain;
using SharedKernel.CQRS;
using SharedKernel.Domain;
using SharedKernel.Events;
using SharedKernel.Messaging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PaymentService.Application.Commands;

public record HandleRazorpayWebhookCommand(string Json, string WebhookSignature) : ICommand;

public class HandleRazorpayWebhookHandler(
    IPaymentRepository repo,
    IEventPublisher events,
    IConfiguration config) : ICommandHandler<HandleRazorpayWebhookCommand>
{
    public async Task<Result> Handle(HandleRazorpayWebhookCommand cmd, CancellationToken ct)
    {
        // Verify webhook signature
        var secret = config["Razorpay:WebhookSecret"]!;
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var computed = Convert.ToHexString(
            hmac.ComputeHash(Encoding.UTF8.GetBytes(cmd.Json))).ToLower();

        if (computed != cmd.WebhookSignature)
            return Result.Failure("Invalid webhook signature.");

        var doc = JsonDocument.Parse(cmd.Json);
        var eventType = doc.RootElement.GetProperty("event").GetString();

        if (eventType == "payment.captured")
        {
            var paymentEntity = doc.RootElement
                .GetProperty("payload").GetProperty("payment").GetProperty("entity");

            var rzpPaymentId = paymentEntity.GetProperty("id").GetString()!;
            var rzpOrderId   = paymentEntity.GetProperty("order_id").GetString()!;

            var payment = await repo.GetByRazorpayOrderIdAsync(rzpOrderId, ct);
            if (payment is not null && payment.Status == PaymentStatus.Processing)
            {
                payment.Complete(rzpPaymentId);
                await repo.UpdateAsync(payment, ct);
            }
        }
        else if (eventType == "payment.failed")
        {
            var paymentEntity = doc.RootElement
                .GetProperty("payload").GetProperty("payment").GetProperty("entity");

            var rzpOrderId    = paymentEntity.GetProperty("order_id").GetString()!;
            var errorDesc     = paymentEntity.GetProperty("error_description").GetString() ?? "Payment failed";

            var payment = await repo.GetByRazorpayOrderIdAsync(rzpOrderId, ct);
            if (payment is not null && payment.Status == PaymentStatus.Processing)
            {
                payment.Fail(errorDesc);
                await repo.UpdateAsync(payment, ct);
                await events.PublishAsync(
                    new PaymentFailedEvent(payment.Id, payment.OrderId, errorDesc, DateTime.UtcNow), ct);
            }
        }

        return Result.Success();
    }
}
