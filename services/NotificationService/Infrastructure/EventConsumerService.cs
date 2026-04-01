using Microsoft.AspNetCore.SignalR;
using NotificationService.Domain;
using NotificationService.Hubs;
using SharedKernel.Events;
using SharedKernel.Messaging;
namespace NotificationService.Infrastructure;

/// <summary>
/// Payload shape published by AuthService's NotificationRelay on the "user.registered" topic.
/// Matches the anonymous object sent via RabbitMQ so it can be deserialized here.
/// </summary>
public record UserRegisteredMessage(Guid UserId, string Email, string FirstName, DateTime OccurredOn);

/// <summary>
/// Hosted background service that subscribes to RabbitMQ integration events and:
/// 1. Sends transactional emails via <see cref="EmailService"/>.
/// 2. Persists in-app notifications to the database.
/// 3. Pushes real-time notifications to connected browser clients via SignalR.
///
/// Subscriptions are set up once at startup. If RabbitMQ is unavailable the service
/// logs a warning and skips that subscription — it does not crash the process.
/// A scoped <see cref="IServiceScopeFactory"/> is used to resolve scoped services
/// (DbContext, EmailService) inside the singleton background service.
/// </summary>
public class EventConsumerService(
    IMessageBus bus,
    IServiceScopeFactory scopeFactory,
    IHubContext<NotificationHub> hubContext,
    ILogger<EventConsumerService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        logger.LogInformation("EventConsumerService starting...");

        // Welcome email on user registration
        await bus.SubscribeAsync<UserRegisteredMessage>("user.registered", async msg =>
        {
            logger.LogInformation("Consumed user.registered for {Email}", msg.Email);
            await using var scope = scopeFactory.CreateAsyncScope();
            var email = scope.ServiceProvider.GetRequiredService<EmailService>();
            await email.SendWelcomeAsync(msg.Email, msg.FirstName);
        }, ct);

        // OTP emails (email verification + password reset)
        await bus.SubscribeAsync<OtpRequestedEvent>("otp.requested", async evt =>
        {
            logger.LogInformation("Consumed otp.requested for {Email} purpose={Purpose}", evt.Email, evt.Purpose);
            await using var scope = scopeFactory.CreateAsyncScope();
            var email = scope.ServiceProvider.GetRequiredService<EmailService>();
            if (evt.Purpose == "email-verification")
                await email.SendEmailVerificationOtpAsync(evt.Email, evt.FirstName, evt.Otp);
            else if (evt.Purpose == "password-reset")
                await email.SendPasswordResetOtpAsync(evt.Email, evt.FirstName, evt.Otp);
        }, ct);

        // Order confirmation email + in-app notification
        await bus.SubscribeAsync<OrderCreatedEvent>("order.created", async evt =>
        {
            logger.LogInformation("Consumed order.created for order {OrderId}", evt.OrderId);
            await using var scope = scopeFactory.CreateAsyncScope();
            var email = scope.ServiceProvider.GetRequiredService<EmailService>();
            var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

            if (!string.IsNullOrEmpty(evt.CustomerEmail))
            {
                var subTotal = evt.Items.Sum(i => i.UnitPrice * i.Quantity);
                var deliveryFee = subTotal >= 500 ? 0m : 49m;
                var taxAmount = Math.Round(subTotal * 0.05m, 2);
                await email.SendOrderConfirmationAsync(
                    evt.CustomerEmail,
                    evt.CustomerFirstName,
                    evt.OrderRef,
                    evt.TotalAmount,
                    evt.Items.Select(i => (i.ProductName, i.Quantity, i.UnitPrice)),
                    deliveryFee: deliveryFee,
                    taxAmount: taxAmount);
            }

            var orderCreatedNotif = new Notification
            {
                UserId = evt.CustomerId,
                Title = "Order Confirmed",
                Message = $"Order #{evt.OrderRef} placed. Total: Rs.{evt.TotalAmount:F2}",
                Type = "success",
                Link = "/orders"
            };
            db.Notifications.Add(orderCreatedNotif);
            await db.SaveChangesAsync(ct);

            await hubContext.Clients.Group(evt.CustomerId.ToString())
                .SendAsync("notification", orderCreatedNotif, ct);
        }, ct);

        // Order status update email + in-app notification.
        // On "Delivered" status, sends a full invoice email instead of a simple status update.
        await bus.SubscribeAsync<OrderStatusChangedEvent>("order.status-changed", async evt =>
        {
            logger.LogInformation("Consumed order.status-changed for order {OrderId} to {Status}", evt.OrderId, evt.NewStatus);
            await using var scope = scopeFactory.CreateAsyncScope();
            var email = scope.ServiceProvider.GetRequiredService<EmailService>();
            var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

            if (!string.IsNullOrEmpty(evt.CustomerEmail))
            {
                if (evt.NewStatus == "Delivered" && evt.Items is { Count: > 0 })
                {
                    await email.SendDeliveryInvoiceAsync(
                        evt.CustomerEmail,
                        evt.CustomerFirstName,
                        evt.OrderRef,
                        evt.Items.Select(i => (i.ProductName, i.Quantity, i.UnitPrice)),
                        evt.DeliveryAddress,
                        subTotal: evt.Items.Sum(i => i.UnitPrice * i.Quantity),
                        deliveryFee: evt.DeliveryFee,
                        taxAmount: evt.TaxAmount,
                        discountAmount: evt.DiscountAmount,
                        totalAmount: evt.TotalAmount,
                        deliveredAt: evt.OccurredOn);
                }
                else
                {
                    await email.SendOrderStatusAsync(evt.CustomerEmail, evt.CustomerFirstName, evt.OrderRef, evt.NewStatus);
                }
            }

            var (title, msg, type) = evt.NewStatus switch
            {
                "Processing"     => ("Order Processing",  $"Your order #{evt.OrderRef} is being prepared.", "info"),
                "Shipped"        => ("Order Shipped",     $"Your order #{evt.OrderRef} has been shipped!", "info"),
                "OutForDelivery" => ("Out for Delivery",  $"Your order #{evt.OrderRef} is out for delivery!", "warning"),
                "Delivered"      => ("Order Delivered",   $"Your order #{evt.OrderRef} has been delivered. Enjoy!", "success"),
                "Cancelled"      => ("Order Cancelled",   $"Your order #{evt.OrderRef} has been cancelled.", "error"),
                _                => ("Order Updated",     $"Your order #{evt.OrderRef} status: {evt.NewStatus}", "info")
            };

            var statusNotif = new Notification
            {
                UserId = evt.CustomerId,
                Title = title,
                Message = msg,
                Type = type,
                Link = "/orders"
            };
            db.Notifications.Add(statusNotif);
            await db.SaveChangesAsync(ct);

            await hubContext.Clients.Group(evt.CustomerId.ToString())
                .SendAsync("notification", statusNotif, ct);
        }, ct);

        // Keep the hosted service alive until the application shuts down
        await Task.Delay(Timeout.Infinite, ct);
    }
}
