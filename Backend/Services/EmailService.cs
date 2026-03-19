using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Backend.Services;

public class EmailService(IConfiguration config, ILogger<EmailService> logger)
{
    private readonly string _host     = config["Email:Host"] ?? "smtp.gmail.com";
    private readonly int    _port     = int.Parse(config["Email:Port"] ?? "587");
    private readonly string _user     = config["Email:Username"] ?? "";
    private readonly string _pass     = config["Email:Password"] ?? "";
    private readonly string _from     = config["Email:From"] ?? "FreshMart <noreply@freshmart.com>";
    private readonly bool   _enabled  = bool.Parse(config["Email:Enabled"] ?? "false");

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

    // ── Templates ────────────────────────────────────────────────────────────

    public Task SendWelcomeAsync(string email, string firstName) =>
        SendAsync(email, firstName, "Welcome to FreshMart!", $"""
            <div style="font-family:sans-serif;max-width:600px;margin:auto;padding:24px">
              <h2 style="color:#16a34a">Welcome to FreshMart, {firstName}! 🛒</h2>
              <p>Your account has been created successfully.</p>
              <p>Start exploring fresh groceries, exclusive deals, and more.</p>
              <a href="http://localhost:4200/products"
                 style="display:inline-block;margin-top:16px;padding:12px 24px;background:#16a34a;color:#fff;border-radius:8px;text-decoration:none;font-weight:600">
                Shop Now
              </a>
              <p style="margin-top:24px;color:#6b7280;font-size:13px">FreshMart — Fresh groceries delivered to your door.</p>
            </div>
        """);

    public Task SendOrderConfirmationAsync(string email, string firstName, string orderId, decimal total, IEnumerable<(string name, int qty, decimal price)> items) =>
        SendAsync(email, firstName, $"Order Confirmed — #{orderId}", $"""
            <div style="font-family:sans-serif;max-width:600px;margin:auto;padding:24px">
              <h2 style="color:#16a34a">Order Confirmed ✅</h2>
              <p>Hi {firstName}, your order <strong>#{orderId}</strong> has been placed successfully.</p>
              <table style="width:100%;border-collapse:collapse;margin:16px 0">
                <thead>
                  <tr style="background:#f3f4f6">
                    <th style="text-align:left;padding:8px;border:1px solid #e5e7eb">Item</th>
                    <th style="text-align:center;padding:8px;border:1px solid #e5e7eb">Qty</th>
                    <th style="text-align:right;padding:8px;border:1px solid #e5e7eb">Price</th>
                  </tr>
                </thead>
                <tbody>
                  {string.Join("", items.Select(i => $"""
                    <tr>
                      <td style="padding:8px;border:1px solid #e5e7eb">{i.name}</td>
                      <td style="text-align:center;padding:8px;border:1px solid #e5e7eb">{i.qty}</td>
                      <td style="text-align:right;padding:8px;border:1px solid #e5e7eb">&#8377;{i.price * i.qty:F2}</td>
                    </tr>
                  """))}
                </tbody>
              </table>
              <p style="font-size:16px;font-weight:600">Total: &#8377;{total:F2}</p>
              <a href="http://localhost:4200/orders"
                 style="display:inline-block;margin-top:16px;padding:12px 24px;background:#16a34a;color:#fff;border-radius:8px;text-decoration:none;font-weight:600">
                Track Order
              </a>
              <p style="margin-top:24px;color:#6b7280;font-size:13px">Estimated delivery: 2 business days.</p>
            </div>
        """);

    public Task SendOrderStatusAsync(string email, string firstName, string orderId, string status) =>
        SendAsync(email, firstName, $"Order #{orderId} — {status}", $"""
            <div style="font-family:sans-serif;max-width:600px;margin:auto;padding:24px">
              <h2 style="color:#16a34a">Order Update 📦</h2>
              <p>Hi {firstName}, your order <strong>#{orderId}</strong> status has been updated to:</p>
              <p style="font-size:20px;font-weight:700;color:#16a34a">{status}</p>
              <a href="http://localhost:4200/orders"
                 style="display:inline-block;margin-top:16px;padding:12px 24px;background:#16a34a;color:#fff;border-radius:8px;text-decoration:none;font-weight:600">
                View Order
              </a>
              <p style="margin-top:24px;color:#6b7280;font-size:13px">Thank you for shopping with FreshMart.</p>
            </div>
        """);

    public Task SendEmailVerificationOtpAsync(string email, string firstName, string otp) =>
        SendAsync(email, firstName, "Verify your FreshMart account", $"""
            <div style="font-family:sans-serif;max-width:600px;margin:auto;padding:24px">
              <h2 style="color:#16a34a">Verify your email &#x2709;</h2>
              <p>Hi {firstName}, thanks for registering with FreshMart!</p>
              <p>Use the OTP below to verify your email address. It expires in <strong>10 minutes</strong>.</p>
              <div style="margin:24px 0;text-align:center">
                <span style="display:inline-block;font-size:36px;font-weight:800;letter-spacing:12px;color:#111827;background:#f3f4f6;padding:16px 32px;border-radius:12px;border:2px dashed #d1d5db">{otp}</span>
              </div>
              <p style="color:#6b7280;font-size:13px">If you did not create an account, you can safely ignore this email.</p>
            </div>
        """);

    public Task SendPasswordResetOtpAsync(string email, string firstName, string otp) =>
        SendAsync(email, firstName, "Reset your FreshMart password", $"""
            <div style="font-family:sans-serif;max-width:600px;margin:auto;padding:24px">
              <h2 style="color:#16a34a">Password Reset &#x1F512;</h2>
              <p>Hi {firstName}, we received a request to reset your password.</p>
              <p>Use the OTP below. It expires in <strong>10 minutes</strong>.</p>
              <div style="margin:24px 0;text-align:center">
                <span style="display:inline-block;font-size:36px;font-weight:800;letter-spacing:12px;color:#111827;background:#f3f4f6;padding:16px 32px;border-radius:12px;border:2px dashed #d1d5db">{otp}</span>
              </div>
              <p style="color:#6b7280;font-size:13px">If you did not request a password reset, ignore this email.</p>
            </div>
        """);
}
