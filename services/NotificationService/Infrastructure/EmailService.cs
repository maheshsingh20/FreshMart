using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace NotificationService.Infrastructure;

/// <summary>
/// Sends transactional emails via MailKit/SMTP.
/// All templates use inline CSS for maximum email client compatibility.
/// </summary>
public class EmailService(IConfiguration config, ILogger<EmailService> logger)
{
    private readonly string _host    = config["Email:Host"] ?? "smtp.gmail.com";
    private readonly int    _port    = int.Parse(config["Email:Port"] ?? "587");
    private readonly string _user    = config["Email:Username"] ?? "";
    private readonly string _pass    = config["Email:Password"] ?? "";
    private readonly string _from    = config["Email:From"] ?? "FreshMart <noreply@freshmart.com>";
    private readonly bool   _enabled = bool.Parse(config["Email:Enabled"] ?? "false");

    // ── Core send ─────────────────────────────────────────────────────────────

    public async Task SendAsync(string toEmail, string toName, string subject, string htmlBody)
    {
        if (!_enabled)
        {
            logger.LogInformation("[Email DISABLED] To: {Email} | Subject: {Subject}", toEmail, subject);
            return;
        }
        try
        {
            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(_from));
            message.To.Add(new MailboxAddress(toName, toEmail));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = htmlBody };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(_host, _port, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_user, _pass);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);

            logger.LogInformation("[Email SENT] To: {Email} | Subject: {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Email FAILED] To: {Email} | Subject: {Subject}", toEmail, subject);
        }
    }

    // ── Welcome ───────────────────────────────────────────────────────────────

    public Task SendWelcomeAsync(string email, string firstName) =>
        SendAsync(email, firstName, "Welcome to FreshMart! 🛒", Wrap($"""
            <div style="text-align:center;padding:8px 0 24px">
              <div style="font-size:56px">🛒</div>
              <h1 style="margin:12px 0 4px;font-size:26px;color:#111827">Welcome, {firstName}!</h1>
              <p style="color:#6b7280;margin:0 0 24px">Your FreshMart account is ready.</p>
            </div>
            <p style="color:#374151;line-height:1.7">
              We're thrilled to have you on board. Explore thousands of fresh groceries,
              exclusive deals, and get doorstep delivery in minutes.
            </p>
            <div style="margin:28px 0;padding:20px;background:#f0fdf4;border-radius:12px;border-left:4px solid #16a34a">
              <p style="margin:0;color:#166534;font-weight:600">🎁 New member offer</p>
              <p style="margin:4px 0 0;color:#166534">Use code <strong>WELCOME10</strong> for 10% off your first order!</p>
            </div>
            {Btn("Start Shopping", "http://localhost:4200/products")}
        """));

    // ── OTP ───────────────────────────────────────────────────────────────────

    public Task SendEmailVerificationOtpAsync(string email, string firstName, string otp) =>
        SendAsync(email, firstName, "Verify your FreshMart account ✉️", Wrap($"""
            <div style="text-align:center;padding:8px 0 24px">
              <div style="font-size:48px">✉️</div>
              <h1 style="margin:12px 0 4px;font-size:24px;color:#111827">Verify your email</h1>
              <p style="color:#6b7280;margin:0">Hi {firstName}, use the code below to verify your account.</p>
            </div>
            <div style="text-align:center;margin:28px 0">
              <div style="display:inline-block;font-size:40px;font-weight:800;letter-spacing:14px;
                color:#111827;background:#f3f4f6;padding:18px 36px;border-radius:14px;
                border:2px dashed #d1d5db">{otp}</div>
            </div>
            <p style="text-align:center;color:#9ca3af;font-size:13px">
              ⏱ This code expires in <strong>10 minutes</strong>. Do not share it with anyone.
            </p>
        """));

    public Task SendPasswordResetOtpAsync(string email, string firstName, string otp) =>
        SendAsync(email, firstName, "Reset your FreshMart password 🔒", Wrap($"""
            <div style="text-align:center;padding:8px 0 24px">
              <div style="font-size:48px">🔒</div>
              <h1 style="margin:12px 0 4px;font-size:24px;color:#111827">Password Reset</h1>
              <p style="color:#6b7280;margin:0">Hi {firstName}, use the code below to reset your password.</p>
            </div>
            <div style="text-align:center;margin:28px 0">
              <div style="display:inline-block;font-size:40px;font-weight:800;letter-spacing:14px;
                color:#111827;background:#f3f4f6;padding:18px 36px;border-radius:14px;
                border:2px dashed #d1d5db">{otp}</div>
            </div>
            <p style="text-align:center;color:#9ca3af;font-size:13px">
              ⏱ Expires in <strong>10 minutes</strong>. If you didn't request this, ignore this email.
            </p>
        """));

    // ── Order Confirmation ────────────────────────────────────────────────────

    public Task SendOrderConfirmationAsync(
        string email, string firstName, string orderRef, decimal total,
        IEnumerable<(string name, int qty, decimal unitPrice)> items,
        string deliveryAddress = "",
        decimal deliveryFee = 0, decimal taxAmount = 0, decimal discountAmount = 0)
    {
        var itemList = items.ToList();
        var subTotal = itemList.Sum(i => i.unitPrice * i.qty);

        var rows = string.Join("", itemList.Select(i => $"""
            <tr>
              <td style="padding:10px 12px;border-bottom:1px solid #f3f4f6;color:#374151">{i.name}</td>
              <td style="padding:10px 12px;border-bottom:1px solid #f3f4f6;text-align:center;color:#6b7280">{i.qty}</td>
              <td style="padding:10px 12px;border-bottom:1px solid #f3f4f6;text-align:right;color:#374151">&#8377;{i.unitPrice:F2}</td>
              <td style="padding:10px 12px;border-bottom:1px solid #f3f4f6;text-align:right;font-weight:600;color:#111827">&#8377;{i.unitPrice * i.qty:F2}</td>
            </tr>
        """));

        var discountRow = discountAmount > 0
            ? $"""<tr><td colspan="3" style="padding:6px 12px;text-align:right;color:#16a34a;font-size:13px">Discount</td>
                   <td style="padding:6px 12px;text-align:right;color:#16a34a;font-size:13px">- &#8377;{discountAmount:F2}</td></tr>"""
            : "";

        var addrBlock = !string.IsNullOrWhiteSpace(deliveryAddress)
            ? $"""
              <div style="margin:20px 0;padding:16px;background:#f9fafb;border-radius:10px;border:1px solid #e5e7eb">
                <p style="margin:0 0 4px;font-size:12px;font-weight:700;color:#9ca3af;text-transform:uppercase;letter-spacing:.05em">Delivering to</p>
                <p style="margin:0;color:#374151;font-size:14px;line-height:1.6">{deliveryAddress}</p>
              </div>
            """ : "";

        return SendAsync(email, firstName, $"Order Confirmed ✅ — #{orderRef}", Wrap($"""
            <div style="text-align:center;padding:8px 0 20px">
              <div style="font-size:48px">✅</div>
              <h1 style="margin:12px 0 4px;font-size:24px;color:#111827">Order Confirmed!</h1>
              <p style="color:#6b7280;margin:0">Hi {firstName}, we've received your order.</p>
            </div>

            <div style="background:#f0fdf4;border-radius:10px;padding:14px 18px;margin-bottom:20px;display:flex;justify-content:space-between;align-items:center">
              <div>
                <p style="margin:0;font-size:12px;color:#6b7280;text-transform:uppercase;letter-spacing:.05em">Order Reference</p>
                <p style="margin:4px 0 0;font-size:20px;font-weight:800;color:#16a34a;letter-spacing:2px">#{orderRef}</p>
              </div>
              <div style="text-align:right">
                <p style="margin:0;font-size:12px;color:#6b7280">Placed on</p>
                <p style="margin:4px 0 0;font-size:14px;font-weight:600;color:#374151">{DateTime.UtcNow:dd MMM yyyy, HH:mm} UTC</p>
              </div>
            </div>

            <table style="width:100%;border-collapse:collapse;font-size:14px">
              <thead>
                <tr style="background:#f9fafb">
                  <th style="padding:10px 12px;text-align:left;color:#6b7280;font-weight:600;border-bottom:2px solid #e5e7eb">Item</th>
                  <th style="padding:10px 12px;text-align:center;color:#6b7280;font-weight:600;border-bottom:2px solid #e5e7eb">Qty</th>
                  <th style="padding:10px 12px;text-align:right;color:#6b7280;font-weight:600;border-bottom:2px solid #e5e7eb">Unit Price</th>
                  <th style="padding:10px 12px;text-align:right;color:#6b7280;font-weight:600;border-bottom:2px solid #e5e7eb">Total</th>
                </tr>
              </thead>
              <tbody>{rows}</tbody>
              <tfoot>
                <tr><td colspan="3" style="padding:8px 12px;text-align:right;color:#6b7280;font-size:13px">Subtotal</td>
                    <td style="padding:8px 12px;text-align:right;color:#374151;font-size:13px">&#8377;{subTotal:F2}</td></tr>
                <tr><td colspan="3" style="padding:4px 12px;text-align:right;color:#6b7280;font-size:13px">Delivery</td>
                    <td style="padding:4px 12px;text-align:right;color:#374151;font-size:13px">{(deliveryFee == 0 ? "<span style='color:#16a34a;font-weight:600'>FREE</span>" : $"&#8377;{deliveryFee:F2}")}</td></tr>
                <tr><td colspan="3" style="padding:4px 12px;text-align:right;color:#6b7280;font-size:13px">Tax (5%)</td>
                    <td style="padding:4px 12px;text-align:right;color:#374151;font-size:13px">&#8377;{taxAmount:F2}</td></tr>
                {discountRow}
                <tr style="background:#f9fafb">
                  <td colspan="3" style="padding:12px;text-align:right;font-weight:700;color:#111827;font-size:15px;border-top:2px solid #e5e7eb">Total Paid</td>
                  <td style="padding:12px;text-align:right;font-weight:800;color:#16a34a;font-size:18px;border-top:2px solid #e5e7eb">&#8377;{total:F2}</td>
                </tr>
              </tfoot>
            </table>

            {addrBlock}

            <div style="margin:20px 0;padding:14px;background:#eff6ff;border-radius:10px;border-left:4px solid #3b82f6">
              <p style="margin:0;color:#1d4ed8;font-size:13px">📦 Estimated delivery: <strong>2 business days</strong></p>
            </div>

            {Btn("Track Your Order", "http://localhost:4200/orders")}
        """));
    }

    // ── Order Status Update ───────────────────────────────────────────────────

    public Task SendOrderStatusAsync(string email, string firstName, string orderRef, string status) =>
        SendAsync(email, firstName, $"Order #{orderRef} — {status}", Wrap($"""
            <div style="text-align:center;padding:8px 0 24px">
              <div style="font-size:48px">{StatusIcon(status)}</div>
              <h1 style="margin:12px 0 4px;font-size:24px;color:#111827">Order Update</h1>
              <p style="color:#6b7280;margin:0">Hi {firstName}, here's the latest on your order.</p>
            </div>
            <div style="text-align:center;margin:20px 0;padding:20px;background:#f9fafb;border-radius:12px">
              <p style="margin:0 0 6px;font-size:13px;color:#9ca3af;text-transform:uppercase;letter-spacing:.05em">Order #{orderRef}</p>
              <p style="margin:0;font-size:22px;font-weight:800;color:{StatusColor(status)}">{StatusLabel(status)}</p>
            </div>
            <p style="color:#374151;line-height:1.7;text-align:center">{StatusMessage(firstName, status)}</p>
            {Btn("View Order", "http://localhost:4200/orders")}
        """));

    // ── Delivery Invoice ──────────────────────────────────────────────────────

    public Task SendDeliveryInvoiceAsync(
        string email, string firstName, string orderRef,
        IEnumerable<(string name, int qty, decimal unitPrice)> items,
        string deliveryAddress,
        decimal subTotal, decimal deliveryFee, decimal taxAmount,
        decimal discountAmount, decimal totalAmount,
        DateTime deliveredAt)
    {
        var itemList = items.ToList();

        var rows = string.Join("", itemList.Select((i, idx) => $"""
            <tr style="background:{(idx % 2 == 0 ? "#ffffff" : "#f9fafb")}">
              <td style="padding:10px 14px;color:#374151;font-size:14px">{i.name}</td>
              <td style="padding:10px 14px;text-align:center;color:#6b7280;font-size:14px">{i.qty}</td>
              <td style="padding:10px 14px;text-align:right;color:#6b7280;font-size:14px">&#8377;{i.unitPrice:F2}</td>
              <td style="padding:10px 14px;text-align:right;font-weight:600;color:#111827;font-size:14px">&#8377;{i.unitPrice * i.qty:F2}</td>
            </tr>
        """));

        var discountRow = discountAmount > 0
            ? $"""<tr><td colspan="3" style="padding:6px 14px;text-align:right;color:#16a34a;font-size:13px">Discount Applied</td>
                   <td style="padding:6px 14px;text-align:right;color:#16a34a;font-size:13px;font-weight:600">- &#8377;{discountAmount:F2}</td></tr>"""
            : "";

        return SendAsync(email, firstName, $"Invoice — FreshMart Order #{orderRef} 🧾", Wrap($"""
            <!-- Invoice Header -->
            <div style="display:flex;justify-content:space-between;align-items:flex-start;margin-bottom:28px;padding-bottom:20px;border-bottom:2px solid #e5e7eb">
              <div>
                <h1 style="margin:0;font-size:28px;font-weight:900;color:#16a34a">FreshMart</h1>
                <p style="margin:4px 0 0;color:#9ca3af;font-size:13px">Fresh groceries, delivered fast</p>
              </div>
              <div style="text-align:right">
                <p style="margin:0;font-size:11px;color:#9ca3af;text-transform:uppercase;letter-spacing:.08em">Invoice</p>
                <p style="margin:4px 0 0;font-size:20px;font-weight:800;color:#111827;letter-spacing:1px">#{orderRef}</p>
                <p style="margin:4px 0 0;font-size:12px;color:#6b7280">{deliveredAt:dd MMM yyyy}</p>
              </div>
            </div>

            <!-- Delivered badge -->
            <div style="text-align:center;margin-bottom:24px">
              <span style="display:inline-block;background:#dcfce7;color:#166534;font-weight:700;font-size:13px;
                padding:6px 18px;border-radius:999px;letter-spacing:.05em">✅ DELIVERED</span>
            </div>

            <!-- Bill to / Deliver to -->
            <div style="display:flex;gap:20px;margin-bottom:24px">
              <div style="flex:1;padding:14px;background:#f9fafb;border-radius:10px;border:1px solid #e5e7eb">
                <p style="margin:0 0 6px;font-size:11px;font-weight:700;color:#9ca3af;text-transform:uppercase;letter-spacing:.08em">Billed To</p>
                <p style="margin:0;font-weight:600;color:#111827;font-size:14px">{firstName}</p>
                <p style="margin:2px 0 0;color:#6b7280;font-size:13px">{email}</p>
              </div>
              <div style="flex:1;padding:14px;background:#f9fafb;border-radius:10px;border:1px solid #e5e7eb">
                <p style="margin:0 0 6px;font-size:11px;font-weight:700;color:#9ca3af;text-transform:uppercase;letter-spacing:.08em">Delivered To</p>
                <p style="margin:0;color:#374151;font-size:13px;line-height:1.6">{deliveryAddress}</p>
              </div>
            </div>

            <!-- Items table -->
            <table style="width:100%;border-collapse:collapse;margin-bottom:4px">
              <thead>
                <tr style="background:#111827">
                  <th style="padding:10px 14px;text-align:left;color:#f9fafb;font-size:12px;font-weight:600;text-transform:uppercase;letter-spacing:.05em;border-radius:8px 0 0 0">Item</th>
                  <th style="padding:10px 14px;text-align:center;color:#f9fafb;font-size:12px;font-weight:600;text-transform:uppercase;letter-spacing:.05em">Qty</th>
                  <th style="padding:10px 14px;text-align:right;color:#f9fafb;font-size:12px;font-weight:600;text-transform:uppercase;letter-spacing:.05em">Unit Price</th>
                  <th style="padding:10px 14px;text-align:right;color:#f9fafb;font-size:12px;font-weight:600;text-transform:uppercase;letter-spacing:.05em;border-radius:0 8px 0 0">Amount</th>
                </tr>
              </thead>
              <tbody>{rows}</tbody>
              <tfoot style="border-top:2px solid #e5e7eb">
                <tr><td colspan="3" style="padding:8px 14px;text-align:right;color:#6b7280;font-size:13px">Subtotal</td>
                    <td style="padding:8px 14px;text-align:right;color:#374151;font-size:13px">&#8377;{subTotal:F2}</td></tr>
                <tr><td colspan="3" style="padding:4px 14px;text-align:right;color:#6b7280;font-size:13px">Delivery Fee</td>
                    <td style="padding:4px 14px;text-align:right;font-size:13px">{(deliveryFee == 0 ? "<span style='color:#16a34a;font-weight:600'>FREE</span>" : $"&#8377;{deliveryFee:F2}")}</td></tr>
                <tr><td colspan="3" style="padding:4px 14px;text-align:right;color:#6b7280;font-size:13px">Tax (5%)</td>
                    <td style="padding:4px 14px;text-align:right;color:#374151;font-size:13px">&#8377;{taxAmount:F2}</td></tr>
                {discountRow}
                <tr style="background:#f0fdf4">
                  <td colspan="3" style="padding:14px;text-align:right;font-weight:700;color:#111827;font-size:16px;border-top:2px solid #16a34a">Total Paid</td>
                  <td style="padding:14px;text-align:right;font-weight:900;color:#16a34a;font-size:22px;border-top:2px solid #16a34a">&#8377;{totalAmount:F2}</td>
                </tr>
              </tfoot>
            </table>

            <p style="margin:24px 0 8px;text-align:center;color:#9ca3af;font-size:12px">
              Thank you for shopping with FreshMart! 🛒 We hope to see you again soon.
            </p>
            {Btn("Shop Again", "http://localhost:4200/products")}

            <p style="margin:20px 0 0;text-align:center;color:#d1d5db;font-size:11px">
              This is a computer-generated invoice. No signature required.
            </p>
        """));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string Wrap(string body) => $"""
        <!DOCTYPE html>
        <html><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"></head>
        <body style="margin:0;padding:0;background:#f3f4f6;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,sans-serif">
          <table width="100%" cellpadding="0" cellspacing="0" style="background:#f3f4f6;padding:32px 16px">
            <tr><td align="center">
              <table width="600" cellpadding="0" cellspacing="0" style="max-width:600px;width:100%;background:#ffffff;border-radius:16px;overflow:hidden;box-shadow:0 4px 24px rgba(0,0,0,.08)">
                <!-- Header bar -->
                <tr><td style="background:linear-gradient(135deg,#16a34a,#15803d);padding:20px 32px">
                  <p style="margin:0;font-size:22px;font-weight:800;color:#ffffff;letter-spacing:-0.5px">🛒 FreshMart</p>
                  <p style="margin:2px 0 0;font-size:12px;color:#bbf7d0">Fresh groceries, delivered fast</p>
                </td></tr>
                <!-- Body -->
                <tr><td style="padding:32px">
                  {body}
                </td></tr>
                <!-- Footer -->
                <tr><td style="background:#f9fafb;padding:20px 32px;border-top:1px solid #e5e7eb;text-align:center">
                  <p style="margin:0;font-size:12px;color:#9ca3af">© {DateTime.UtcNow.Year} FreshMart. All rights reserved.</p>
                  <p style="margin:4px 0 0;font-size:11px;color:#d1d5db">You're receiving this because you have an account with us.</p>
                </td></tr>
              </table>
            </td></tr>
          </table>
        </body></html>
    """;

    private static string Btn(string text, string url) => $"""
        <div style="text-align:center;margin:28px 0 8px">
          <a href="{url}" style="display:inline-block;padding:13px 32px;background:#16a34a;color:#ffffff;
            text-decoration:none;font-weight:700;font-size:15px;border-radius:10px;
            letter-spacing:.02em">{text}</a>
        </div>
    """;

    private static string StatusIcon(string status) => status switch
    {
        "Processing"     => "⚙️",
        "Shipped"        => "🚚",
        "OutForDelivery" => "🛵",
        "Delivered"      => "✅",
        "Cancelled"      => "❌",
        "Refunded"       => "💰",
        _                => "📦"
    };

    private static string StatusColor(string status) => status switch
    {
        "Delivered"      => "#16a34a",
        "Cancelled"      => "#dc2626",
        "OutForDelivery" => "#d97706",
        _                => "#2563eb"
    };

    private static string StatusLabel(string status) => status switch
    {
        "Processing"     => "Being Prepared",
        "Shipped"        => "Shipped",
        "OutForDelivery" => "Out for Delivery",
        "Delivered"      => "Delivered",
        "Cancelled"      => "Cancelled",
        "Refunded"       => "Refunded",
        _                => status
    };

    private static string StatusMessage(string firstName, string status) => status switch
    {
        "Processing"     => $"Great news, {firstName}! Your order is being carefully packed and will be on its way soon.",
        "Shipped"        => $"Your order is on the move, {firstName}! It has been handed over to our delivery partner.",
        "OutForDelivery" => $"Almost there, {firstName}! Your order is out for delivery and will reach you today.",
        "Delivered"      => $"Your order has been delivered, {firstName}! We hope you enjoy your fresh groceries. 🎉",
        "Cancelled"      => $"We're sorry, {firstName}. Your order has been cancelled. If you paid online, a refund will be processed within 5–7 business days.",
        _                => $"Hi {firstName}, your order status has been updated to <strong>{status}</strong>."
    };
}
