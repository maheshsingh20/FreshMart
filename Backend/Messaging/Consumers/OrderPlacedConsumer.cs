using Backend.Services;
using MassTransit;

namespace Backend.Messaging.Consumers;

public class OrderPlacedConsumer(IServiceScopeFactory scopeFactory, ILogger<OrderPlacedConsumer> logger)
    : IConsumer<OrderPlacedMessage>
{
    public async Task Consume(ConsumeContext<OrderPlacedMessage> context)
    {
        var msg = context.Message;
        logger.LogInformation("[MQ] OrderPlaced consumed — Order {Ref}", msg.OrderRef);

        await using var scope = scopeFactory.CreateAsyncScope();
        var email = scope.ServiceProvider.GetRequiredService<EmailService>();
        var notif = scope.ServiceProvider.GetRequiredService<NotificationService>();

        await email.SendOrderConfirmationAsync(
            msg.CustomerEmail, msg.CustomerFirstName, msg.OrderRef, msg.Total,
            msg.Items.Select(i => (i.ProductName, i.Quantity, i.UnitPrice)));

        var paymentNote = msg.IsPaid ? "Payment confirmed." : "Cash on delivery.";
        await notif.SendToUserAsync(
            msg.CustomerId,
            msg.IsPaid ? "Payment Successful \u2705" : "Order Placed \U0001F6D2",
            $"Order #{msg.OrderRef} confirmed. Total: \u20b9{msg.Total:F2}. {paymentNote}",
            "success", $"/orders/{msg.OrderId}/track");

        await notif.SendToRoleAsync("Admin", "New Order Received",
            $"Order #{msg.OrderRef} \u2014 \u20b9{msg.Total:F2}", "order", "/admin/orders");
        await notif.SendToRoleAsync("StoreManager", "New Order Received",
            $"Order #{msg.OrderRef} \u2014 \u20b9{msg.Total:F2}", "order", "/admin/orders");
    }
}
