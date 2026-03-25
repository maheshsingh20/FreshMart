using Razorpay.Api;
using System.Security.Cryptography;
using System.Text;

namespace PaymentService.Infrastructure.Services;

public interface IRazorpayPaymentService
{
    /// <summary>Creates a Razorpay order and returns the razorpay order id.</summary>
    Task<string> CreateOrderAsync(decimal amount, Guid internalOrderId, CancellationToken ct);

    /// <summary>Verifies the HMAC-SHA256 signature from the frontend callback.</summary>
    bool VerifySignature(string razorpayOrderId, string razorpayPaymentId, string signature);

    /// <summary>Issues a full refund for a captured payment.</summary>
    Task RefundAsync(string razorpayPaymentId, CancellationToken ct);
}

public class RazorpayPaymentService(IConfiguration config) : IRazorpayPaymentService
{
    private RazorpayClient Client =>
        new(config["Razorpay:KeyId"]!, config["Razorpay:KeySecret"]!);

    public Task<string> CreateOrderAsync(decimal amount, Guid internalOrderId, CancellationToken ct)
    {
        var options = new Dictionary<string, object>
        {
            { "amount",   (long)(amount * 100) }, // paise
            { "currency", "INR" },
            { "receipt",  $"order_{internalOrderId.ToString()[..8]}_{DateTime.UtcNow.Ticks}" },
            { "notes",    new Dictionary<string, string> { ["orderId"] = internalOrderId.ToString() } }
        };

        var rzpOrder = Client.Order.Create(options);
        return Task.FromResult(rzpOrder["id"].ToString());
    }

    public bool VerifySignature(string razorpayOrderId, string razorpayPaymentId, string signature)
    {
        var secret = config["Razorpay:KeySecret"]!;
        var payload = $"{razorpayOrderId}|{razorpayPaymentId}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var computed = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload))).ToLower();
        return computed == signature;
    }

    public Task RefundAsync(string razorpayPaymentId, CancellationToken ct)
    {
        // Fetch the payment entity then call Refund() on it
        var payment = Client.Payment.Fetch(razorpayPaymentId);
        payment.Refund();
        return Task.CompletedTask;
    }
}
