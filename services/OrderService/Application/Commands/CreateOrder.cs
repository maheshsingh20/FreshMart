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

public class CreateOrderHandler(
    IOrderRepository repo,
    IEventPublisher events,
    IHttpClientFactory httpClientFactory,
    IConfiguration config,
    ILogger<CreateOrderHandler> logger)
    : ICommandHandler<CreateOrderCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateOrderCommand cmd, CancellationToken ct)
    {
        // Validate stock availability before creating the order
        var productServiceUrl = config["Services:ProductService"] ?? "http://product-service:8080";
        var client = httpClientFactory.CreateClient();

        foreach (var item in cmd.Items)
        {
            try
            {
                var product = await client.GetFromJsonAsync<ProductStockDto>(
                    $"{productServiceUrl}/api/v1/products/{item.ProductId}", ct);

                if (product is null)
                    return Result<Guid>.Failure($"Product '{item.ProductName}' not found.");

                if (product.StockQuantity < item.Quantity)
                    return Result<Guid>.Failure(
                        $"'{item.ProductName}' has only {product.StockQuantity} unit(s) available. You requested {item.Quantity}.");
            }
            catch (Exception ex)
            {
                logger.LogWarning("Could not verify stock for product {ProductId}: {Message}", item.ProductId, ex.Message);
                // Continue — don't block order if ProductService is temporarily unavailable
            }
        }

        var items = cmd.Items.Select(i => (i.ProductId, i.ProductName, i.Quantity, i.UnitPrice));
        var order = Order.Create(cmd.CustomerId, cmd.DeliveryAddress, items, notes: cmd.Notes,
            customerEmail: cmd.CustomerEmail, customerFirstName: cmd.CustomerFirstName);

        await repo.AddAsync(order, ct);

        // Deduct stock from ProductService for each item
        foreach (var item in cmd.Items)
        {
            try
            {
                var response = await client.PatchAsJsonAsync(
                    $"{productServiceUrl}/api/v1/products/{item.ProductId}/deduct-stock",
                    new { quantity = item.Quantity }, ct);

                if (!response.IsSuccessStatusCode)
                    logger.LogWarning("Failed to deduct stock for product {ProductId}: {Status}",
                        item.ProductId, response.StatusCode);
            }
            catch (Exception ex)
            {
                logger.LogError("Error deducting stock for product {ProductId}: {Message}", item.ProductId, ex.Message);
            }
        }

        // Publish OrderCreated event to trigger notification + payment saga
        await events.PublishAsync(new OrderCreatedEvent(
            order.Id, order.CustomerId, order.TotalAmount,
            cmd.Items.Select(i => new OrderItemDto(i.ProductId, i.ProductName, i.Quantity, i.UnitPrice)).ToList(),
            DateTime.UtcNow), ct);

        return Result<Guid>.Success(order.Id);
    }
}

// Minimal DTO to read stock from ProductService
file record ProductStockDto(int StockQuantity);
