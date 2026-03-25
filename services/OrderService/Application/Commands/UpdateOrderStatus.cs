using OrderService.Domain;
using SharedKernel.CQRS;
using SharedKernel.Domain;

namespace OrderService.Application.Commands;

public record ConfirmPaymentCommand(Guid OrderId) : ICommand;
public record FailPaymentCommand(Guid OrderId) : ICommand;
public record CancelOrderCommand(Guid OrderId, string Reason) : ICommand;
public record DeliverOrderCommand(Guid OrderId) : ICommand;
public record UpdateOrderStatusCommand(Guid OrderId, string Status) : ICommand;

public class ConfirmPaymentHandler(IOrderRepository repo) : ICommandHandler<ConfirmPaymentCommand>
{
    public async Task<Result> Handle(ConfirmPaymentCommand cmd, CancellationToken ct)
    {
        var order = await repo.GetByIdAsync(cmd.OrderId, ct);
        if (order is null) return Result.Failure("Order not found.");
        order.ConfirmPayment();
        await repo.UpdateAsync(order, ct);
        return Result.Success();
    }
}

public class FailPaymentHandler(IOrderRepository repo) : ICommandHandler<FailPaymentCommand>
{
    public async Task<Result> Handle(FailPaymentCommand cmd, CancellationToken ct)
    {
        var order = await repo.GetByIdAsync(cmd.OrderId, ct);
        if (order is null) return Result.Failure("Order not found.");
        order.FailPayment();
        await repo.UpdateAsync(order, ct);
        return Result.Success();
    }
}

public class CancelOrderHandler(IOrderRepository repo) : ICommandHandler<CancelOrderCommand>
{
    public async Task<Result> Handle(CancelOrderCommand cmd, CancellationToken ct)
    {
        var order = await repo.GetByIdAsync(cmd.OrderId, ct);
        if (order is null) return Result.Failure("Order not found.");
        order.Cancel(cmd.Reason);
        await repo.UpdateAsync(order, ct);
        return Result.Success();
    }
}

public class UpdateOrderStatusHandler(IOrderRepository repo) : ICommandHandler<UpdateOrderStatusCommand>
{
    public async Task<Result> Handle(UpdateOrderStatusCommand cmd, CancellationToken ct)
    {
        var order = await repo.GetByIdAsync(cmd.OrderId, ct);
        if (order is null) return Result.Failure("Order not found.");

        switch (cmd.Status)
        {
            case "Processing": order.StartProcessing(); break;
            case "Shipped": order.Ship(); break;
            case "OutForDelivery": order.OutForDelivery(); break;
            case "Delivered": order.Deliver(); break;
            default: return Result.Failure($"Unknown status: {cmd.Status}");
        }

        await repo.UpdateAsync(order, ct);
        return Result.Success();
    }
}
