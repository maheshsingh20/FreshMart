using Backend.Services;
using MassTransit;

namespace Backend.Messaging.Consumers;

public class OrderStatusChangedConsumer(IServiceScopeFactory scopeFactory, ILogger<OrderStatusChangedConsumer> logger)
    : IConsumer<OrderStatusChangedMessage>
{
    public async Task Consume(ConsumeContext<OrderStatusChangedMessage> context)
    {
        var msg = context.Message;
        logger.LogInformation("[MQ] OrderStatusChanged consumed — Order {Ref} \u2192 {Status}", msg.OrderRef, msg.NewStatus);

        await using var scope = scopeFactory.CreateAsyncScope();
        var email = scope.ServiceProvider.GetRequiredService<EmailService>();
        var notif = scope.ServiceProvider.GetRequiredService<NotificationService>();

        await email.SendOrderStatusAsync(msg.CustomerEmail, msg.CustomerFirstName, msg.OrderRef, msg.NewStatus);

        var (title, body, type) = msg.NewStatus switch
        {
            "Processing"     => ("Order Processing",  $"Your order #{msg.OrderRef} is being prepared.", "info"),
            "Shipped"        => ("Order Shipped",     $"Your order #{msg.OrderRef} has been shipped!", "info"),
            "OutForDelivery" => ("Out for Delivery",  $"Your order #{msg.OrderRef} is out for delivery!", "warning"),
            "Delivered"      => ("Order Delivered",   $"Your order #{msg.OrderRef} has been delivered. Enjoy!", "success"),
            "Cancelled"      => ("Order Cancelled",   $"Your order #{msg.OrderRef} has been cancelled.", "error"),
            _                => ("Order Updated",     $"Your order #{msg.OrderRef} status: {msg.NewStatus}", "info")
        };
        await notif.SendToUserAsync(msg.CustomerId, title, body, type, $"/orders/{msg.OrderId}/track");

        if (msg.NewStatus is "Shipped" or "Processing")
            await notif.SendToRoleAsync("DeliveryDriver", "New Delivery Available",
                $"Order #{msg.OrderRef} is ready for pickup.", "order", "/delivery");
    }
}
