using Microsoft.Extensions.Configuration;
using SharedKernel.Events;
using SharedKernel.Messaging;
using Stripe;

namespace PaymentService.Infrastructure.Services;

public interface IStripePaymentService
{
    Task<(string intentId, string clientSecret)> CreatePaymentIntentAsync(
        decimal amount, string paymentMethodId, string orderId, CancellationToken ct);
    Task RefundAsync(string chargeId, string reason, CancellationToken ct);
}

public class StripePaymentService(IConfiguration config, IEventPublisher events) : IStripePaymentService
{
    public async Task<(string intentId, string clientSecret)> CreatePaymentIntentAsync(
        decimal amount, string paymentMethodId, string orderId, CancellationToken ct)
    {
        StripeConfiguration.ApiKey = config["Stripe:SecretKey"];

        var options = new PaymentIntentCreateOptions
        {
            Amount = (long)(amount * 100), // Stripe uses cents
            Currency = "usd",
            PaymentMethod = paymentMethodId,
            Confirm = true,
            Metadata = new Dictionary<string, string> { ["orderId"] = orderId },
            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
            {
                Enabled = true,
                AllowRedirects = "never"
            }
        };

        var service = new PaymentIntentService();
        var intent = await service.CreateAsync(options, cancellationToken: ct);

        if (intent.Status == "succeeded")
        {
            await events.PublishAsync(new PaymentCompletedEvent(
                Guid.NewGuid(), Guid.Parse(orderId), amount,
                intent.LatestChargeId, DateTime.UtcNow), ct);
        }

        return (intent.Id, intent.ClientSecret);
    }

    public async Task RefundAsync(string chargeId, string reason, CancellationToken ct)
    {
        StripeConfiguration.ApiKey = config["Stripe:SecretKey"];
        var service = new RefundService();
        await service.CreateAsync(new RefundCreateOptions
        {
            Charge = chargeId,
            Reason = "requested_by_customer"
        }, cancellationToken: ct);
    }
}
