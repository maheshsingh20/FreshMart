using Backend.Services;
using MassTransit;

namespace Backend.Messaging.Consumers;

public class StockAlertConsumer(IServiceScopeFactory scopeFactory, ILogger<StockAlertConsumer> logger)
    : IConsumer<StockAlertMessage>
{
    public async Task Consume(ConsumeContext<StockAlertMessage> context)
    {
        var msg = context.Message;
        logger.LogInformation("[MQ] StockAlert consumed — Product '{Name}', Stock: {Stock}", msg.ProductName, msg.RemainingStock);

        await using var scope = scopeFactory.CreateAsyncScope();
        var notif = scope.ServiceProvider.GetRequiredService<NotificationService>();

        if (msg.RemainingStock == 0)
        {
            await notif.SendToRoleAsync("Admin", "\u26A0\uFE0F Out of Stock",
                $"'{msg.ProductName}' is now out of stock. Restock immediately.", "warning", "/admin/products");
            await notif.SendToRoleAsync("StoreManager", "\u26A0\uFE0F Out of Stock",
                $"'{msg.ProductName}' is now out of stock. Restock immediately.", "warning", "/admin/products");
        }
        else if (msg.RemainingStock <= 5)
        {
            await notif.SendToRoleAsync("Admin", "\U0001F4E6 Low Stock Alert",
                $"'{msg.ProductName}' has only {msg.RemainingStock} unit{(msg.RemainingStock == 1 ? "" : "s")} left.", "warning", "/admin/products");
            await notif.SendToRoleAsync("StoreManager", "\U0001F4E6 Low Stock Alert",
                $"'{msg.ProductName}' has only {msg.RemainingStock} unit{(msg.RemainingStock == 1 ? "" : "s")} left.", "warning", "/admin/products");
        }
    }
}
