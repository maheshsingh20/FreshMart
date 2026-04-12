using FluentValidation;
using OrderService.Domain;
using SharedKernel.CQRS;
using SharedKernel.Domain;
using SharedKernel.Messaging;
using SharedKernel.Events;

namespace OrderService.Application.Commands;

/// <summary>
/// Command that encapsulates everything needed to place a new order.
/// Carries the customer identity, delivery destination, line items, and optional
/// customer contact details used to personalise notification emails.
/// Implements <see cref="ICommand{Guid}"/> so MediatR routes it to
/// <see cref="CreateOrderHandler"/> and returns the new order's ID on success.
/// </summary>
public record CreateOrderCommand(
    Guid CustomerId,
    string DeliveryAddress,
    List<OrderItemRequest> Items,
    string? Notes,
    string CustomerEmail = "",
    string CustomerFirstName = "") : ICommand<Guid>;

/// <summary>
/// A single line item within a <see cref="CreateOrderCommand"/>.
/// Captures the product snapshot at the time of ordering so the order record
/// remains accurate even if the product's name or price changes later.
/// </summary>
public record OrderItemRequest(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice);

/// <summary>
/// FluentValidation rules for <see cref="CreateOrderCommand"/>.
/// Ensures the command is structurally valid before the handler performs any
/// I/O, providing fast, descriptive error messages to the API caller.
/// </summary>
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

/// <summary>
/// MediatR command handler that orchestrates order creation.
/// The handler performs three sequential operations:
/// <list type="number">
///   <item>Stock validation — calls ProductService to confirm each item is available.</item>
///   <item>Order persistence — creates the domain <see cref="Order"/> aggregate and saves it.</item>
///   <item>Stock deduction — calls ProductService to atomically reduce inventory.</item>
/// </list>
/// Stock validation failures return a <see cref="Result{T}"/> failure so the API
/// can return a 400 with a user-friendly message. ProductService unavailability is
/// treated as a soft failure (logged, not blocking) to maintain resilience.
/// An <see cref="OrderCreatedEvent"/> is published after persistence to trigger
/// downstream workflows (notifications, payment saga).
/// </summary>
public class CreateOrderHandler(
    IOrderRepository repo,
    IEventPublisher events,
    IHttpClientFactory httpClientFactory,
    IConfiguration config,
    ILogger<CreateOrderHandler> logger)
    : ICommandHandler<CreateOrderCommand, Guid>
{
    /// <summary>
    /// Executes the order creation workflow: validate stock → persist order →
    /// deduct stock → publish domain event.
    /// </summary>
    /// <param name="cmd">The validated create-order command.</param>
    /// <param name="ct">Cancellation token propagated from the HTTP request.</param>
    /// <returns>
    /// <see cref="Result{Guid}.Success"/> containing the new order ID, or
    /// <see cref="Result{Guid}.Failure"/> with a descriptive message if stock
    /// is insufficient or a product is not found.
    /// </returns>
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

/// <summary>
/// Minimal DTO used to deserialise the stock quantity from a ProductService response.
/// Declared as a file-scoped type to keep it private to this compilation unit —
/// it is an implementation detail of the handler, not a shared contract.
/// </summary>
file record ProductStockDto(int StockQuantity);
