using FluentValidation;
using OrderService.Domain;
using SharedKernel.CQRS;
using SharedKernel.Domain;
using SharedKernel.Messaging;
using SharedKernel.Events;

namespace OrderService.Application.Commands;

public record CreateOrderCommand(
    Guid CustomerId,
    string DeliveryAddress,
    List<OrderItemRequest> Items,
    string? Notes,
    string CustomerEmail = "",
    string CustomerFirstName = "") : ICommand<Guid>;

public record OrderItemRequest(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice);

public class CreateOrderValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.DeliveryAddress).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Items).NotEmpty().WithMessage("Order must have at least one item.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.Quantity).GreaterThan(0);
            item.RuleFor(x => x.UnitPrice).GreaterThan(0);
        });
    }
}

public class CreateOrderHandler(IOrderRepository repo, IEventPublisher events)
    : ICommandHandler<CreateOrderCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateOrderCommand cmd, CancellationToken ct)
    {
        var items = cmd.Items.Select(i => (i.ProductId, i.ProductName, i.Quantity, i.UnitPrice));
        var order = Order.Create(cmd.CustomerId, cmd.DeliveryAddress, items, notes: cmd.Notes,
            customerEmail: cmd.CustomerEmail, customerFirstName: cmd.CustomerFirstName);

        await repo.AddAsync(order, ct);

        // Publish OrderCreated event to trigger payment saga
        await events.PublishAsync(new OrderCreatedEvent(
            order.Id, order.CustomerId, order.TotalAmount,
            cmd.Items.Select(i => new OrderItemDto(i.ProductId, i.ProductName, i.Quantity, i.UnitPrice)).ToList(),
            DateTime.UtcNow), ct);

        return Result<Guid>.Success(order.Id);
    }
}
