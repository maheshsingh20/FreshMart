using Razorpay.Api;
using System.Security.Cryptography;
using System.Text;

namespace PaymentService.Infrastructure.Services;

/// <summary>
/// Abstraction over the Razorpay payment gateway.
/// Decoupling behind an interface allows the payment application layer to be
/// tested with a mock/stub without making real API calls, and makes it
/// straightforward to swap payment providers in the future.
/// </summary>
public interface IRazorpayPaymentService
{
    /// <summary>
    /// Creates a Razorpay order on the Razorpay platform and returns the Razorpay order ID.
    /// The order ID is required by the frontend Razorpay SDK to open the checkout modal.
    /// Amount is converted from INR to paise (×100) internally.
    /// </summary>
    /// <param name="amount">Order amount in INR (will be converted to paise).</param>
    /// <param name="internalOrderId">The internal order ID, stored in the Razorpay order notes for reconciliation.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The Razorpay order ID string (e.g. <c>order_XXXXXXXXXX</c>).</returns>
    Task<string> CreateOrderAsync(decimal amount, Guid internalOrderId, CancellationToken ct);

    /// <summary>
    /// Verifies the HMAC-SHA256 signature returned by the Razorpay frontend SDK
    /// after the customer completes payment. The signature is computed over
    /// <c>razorpayOrderId|razorpayPaymentId</c> using the Razorpay key secret.
    /// This is the primary fraud-prevention mechanism — never skip this check.
    /// </summary>
    /// <param name="razorpayOrderId">The Razorpay order ID from the checkout callback.</param>
    /// <param name="razorpayPaymentId">The Razorpay payment ID from the checkout callback.</param>
    /// <param name="signature">The HMAC-SHA256 signature from the checkout callback.</param>
    /// <returns><c>true</c> if the signature is valid; <c>false</c> if it has been tampered with.</returns>
    bool VerifySignature(string razorpayOrderId, string razorpayPaymentId, string signature);

    /// <summary>
    /// Issues a full refund for a captured Razorpay payment.
    /// Fetches the payment entity from Razorpay and calls the Refund API.
    /// Partial refunds are not currently supported.
    /// </summary>
    /// <param name="razorpayPaymentId">The Razorpay payment ID to refund (e.g. <c>pay_XXXXXXXXXX</c>).</param>
    /// <param name="ct">Cancellation token.</param>
    Task RefundAsync(string razorpayPaymentId, CancellationToken ct);
}

/// <summary>
/// Concrete implementation of <see cref="IRazorpayPaymentService"/> that communicates
/// with the Razorpay REST API using the official <c>Razorpay.Api</c> SDK.
/// The <see cref="RazorpayClient"/> is constructed on each call (not cached) because
/// the SDK is not thread-safe and the configuration values are read from
/// <see cref="IConfiguration"/> to support environment-specific keys.
/// </summary>
public class RazorpayPaymentService(IConfiguration config) : IRazorpayPaymentService
{
    /// <summary>
    /// Creates a fresh <see cref="RazorpayClient"/> using the key ID and secret
    /// from application configuration. A new instance is created per call to
    /// avoid thread-safety issues with the underlying SDK.
    /// </summary>
    private RazorpayClient Client =>
        new(config["Razorpay:KeyId"]!, config["Razorpay:KeySecret"]!);

    /// <summary>
    /// Creates a Razorpay order with the specified amount and links it to the
    /// internal order via the <c>notes</c> field. The receipt is a short unique
    /// string combining the order ID prefix and a timestamp tick for traceability
    /// in the Razorpay dashboard.
    /// </summary>
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

    /// <summary>
    /// Computes the expected HMAC-SHA256 signature over
    /// <c>razorpayOrderId|razorpayPaymentId</c> using the Razorpay key secret,
    /// then performs a constant-time string comparison against the provided signature.
    /// This prevents timing attacks that could allow an attacker to forge signatures.
    /// </summary>
    public bool VerifySignature(string razorpayOrderId, string razorpayPaymentId, string signature)
    {
        var secret = config["Razorpay:KeySecret"]!;
        var payload = $"{razorpayOrderId}|{razorpayPaymentId}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var computed = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload))).ToLower();
        return computed == signature;
    }

    /// <summary>
    /// Fetches the Razorpay payment entity by ID and calls the Razorpay Refund API
    /// to issue a full refund. The refund is processed asynchronously by Razorpay;
    /// the actual credit to the customer's account may take 5–7 business days.
    /// </summary>
    public Task RefundAsync(string razorpayPaymentId, CancellationToken ct)
    {
        // Fetch the payment entity then call Refund() on it
        var payment = Client.Payment.Fetch(razorpayPaymentId);
        payment.Refund();
        return Task.CompletedTask;
    }
}
