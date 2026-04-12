using OrderService.Domain;
using SharedKernel.CQRS;

namespace OrderService.Application.Queries;

/// <summary>
/// Read model DTO representing a complete order as returned by the API.
/// Captures a snapshot of the order at query time, including computed financial
/// totals (subtotal, delivery fee, tax, discount, grand total) and the full
/// list of line items. Using a dedicated DTO decouples the API contract from
/// the domain aggregate, allowing each to evolve independently.
/// </summary>
public record OrderDto(Guid Id, Guid CustomerId, string CustomerEmail, string CustomerFirstName,
    string Status, decimal SubTotal,
    decimal DeliveryFee, decimal TaxAmount, decimal DiscountAmount, decimal TotalAmount, string DeliveryAddress,
    string? Notes, DateTime CreatedAt, DateTime? EstimatedDelivery, DateTime? DeliveredAt,
    List<OrderItemDto> Items);

/// <summary>
/// Read model DTO for a single order line item.
/// <c>TotalPrice</c> is the pre-computed line total (Quantity × UnitPrice),
/// stored on the aggregate so it is consistent even if pricing logic changes.
/// </summary>
public record OrderItemDto(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice, decimal TotalPrice);

/// <summary>
/// Query to retrieve a single order by its unique identifier.
/// Returns <c>null</c> when the order does not exist, allowing the controller
/// to return a 404 without throwing an exception.
/// </summary>
public record GetOrderByIdQuery(Guid OrderId) : IQuery<OrderDto?>;

/// <summary>
/// Query to retrieve a paginated list of orders for a specific customer.
/// Used by the customer-facing "My Orders" page.
/// </summary>
public record GetCustomerOrdersQuery(Guid CustomerId, int Page = 1, int PageSize = 10) : IQuery<CustomerOrdersResponse>;

/// <summary>
/// Paginated response wrapper for customer order queries.
/// Includes the total count so the frontend can render pagination controls
/// without issuing a separate count query.
/// </summary>
public record CustomerOrdersResponse(IEnumerable<OrderDto> Orders, int Total, int Page, int PageSize);

/// <summary>
/// Handles <see cref="GetOrderByIdQuery"/> by loading the order aggregate from
/// the repository and projecting it to a <see cref="OrderDto"/>.
/// </summary>
public class GetOrderByIdHandler(IOrderRepository repo) : IQueryHandler<GetOrderByIdQuery, OrderDto?>
{
    /// <summary>
    /// Fetches the order by ID and maps it to the read model.
    /// Returns <c>null</c> if no order with the given ID exists.
    /// </summary>
    public async Task<OrderDto?> Handle(GetOrderByIdQuery q, CancellationToken ct)
    {
        var order = await repo.GetByIdAsync(q.OrderId, ct);
        return order is null ? null : MapToDto(order);
    }

    private static OrderDto MapToDto(Order o) =>
        new(o.Id, o.CustomerId, o.CustomerEmail, o.CustomerFirstName,
            o.Status.ToString(), o.SubTotal, o.DeliveryFee,
            o.TaxAmount, 0m, o.TotalAmount, o.DeliveryAddress, o.Notes, o.CreatedAt,
            o.EstimatedDelivery, o.DeliveredAt,
            o.Items.Select(i => new OrderItemDto(i.ProductId, i.ProductName, i.Quantity, i.UnitPrice, i.TotalPrice)).ToList());
}

/// <summary>
/// Handles <see cref="GetCustomerOrdersQuery"/> by fetching a page of orders
/// for the specified customer and the total count for pagination metadata.
/// </summary>
public class GetCustomerOrdersHandler(IOrderRepository repo)
    : IQueryHandler<GetCustomerOrdersQuery, CustomerOrdersResponse>
{
    /// <summary>
    /// Returns a paginated slice of the customer's order history, newest first.
    /// </summary>
    public async Task<CustomerOrdersResponse> Handle(GetCustomerOrdersQuery q, CancellationToken ct)
    {
        var orders = await repo.GetByCustomerAsync(q.CustomerId, q.Page, q.PageSize, ct);
        var total = await repo.GetTotalByCustomerAsync(q.CustomerId, ct);
        return new CustomerOrdersResponse(orders.Select(MapToDto), total, q.Page, q.PageSize);
    }

    private static OrderDto MapToDto(Order o) =>
        new(o.Id, o.CustomerId, o.CustomerEmail, o.CustomerFirstName,
            o.Status.ToString(), o.SubTotal, o.DeliveryFee,
            o.TaxAmount, 0m, o.TotalAmount, o.DeliveryAddress, o.Notes, o.CreatedAt,
            o.EstimatedDelivery, o.DeliveredAt,
            o.Items.Select(i => new OrderItemDto(i.ProductId, i.ProductName, i.Quantity, i.UnitPrice, i.TotalPrice)).ToList());
}

/// <summary>
/// Query to retrieve all orders matching a specific status string.
/// Used by admin dashboards and the driver app to filter the order queue
/// (e.g. show only "OutForDelivery" orders on the driver's route list).
/// </summary>
public record GetOrdersByStatusQuery(string Status) : IQuery<IEnumerable<OrderDto>>;

/// <summary>
/// Handles <see cref="GetOrdersByStatusQuery"/> by parsing the status string into
/// the <see cref="OrderStatus"/> enum and delegating to the repository.
/// Returns an empty collection if the status string is not a valid enum value,
/// rather than throwing, to keep the API response predictable.
/// </summary>
public class GetOrdersByStatusHandler(IOrderRepository repo)
    : IQueryHandler<GetOrdersByStatusQuery, IEnumerable<OrderDto>>
{
    /// <inheritdoc/>
    public async Task<IEnumerable<OrderDto>> Handle(GetOrdersByStatusQuery q, CancellationToken ct)
    {
        if (!Enum.TryParse<OrderStatus>(q.Status, out var status)) return [];
        var orders = await repo.GetByStatusAsync(status, ct);
        return orders.Select(o => new OrderDto(
            o.Id, o.CustomerId, o.CustomerEmail, o.CustomerFirstName,
            o.Status.ToString(), o.SubTotal, o.DeliveryFee,
            o.TaxAmount, 0m, o.TotalAmount, o.DeliveryAddress, o.Notes, o.CreatedAt,
            o.EstimatedDelivery, o.DeliveredAt,
            o.Items.Select(i => new OrderItemDto(i.ProductId, i.ProductName, i.Quantity, i.UnitPrice, i.TotalPrice)).ToList()));
    }
}

/// <summary>
/// Query to retrieve all orders across all customers, paginated.
/// Restricted to Admin and StoreManager roles via the controller.
/// </summary>
public record GetAllOrdersQuery(int Page = 1, int PageSize = 50) : IQuery<CustomerOrdersResponse>;

/// <summary>
/// Handles <see cref="GetAllOrdersQuery"/> for the admin back-office order list.
/// Reuses the same <see cref="CustomerOrdersResponse"/> wrapper so the frontend
/// pagination component works identically for both customer and admin views.
/// </summary>
public class GetAllOrdersHandler(IOrderRepository repo)
    : IQueryHandler<GetAllOrdersQuery, CustomerOrdersResponse>
{
    /// <inheritdoc/>
    public async Task<CustomerOrdersResponse> Handle(GetAllOrdersQuery q, CancellationToken ct)
    {
        var orders = await repo.GetAllAsync(q.Page, q.PageSize, ct);
        var total = await repo.GetAllCountAsync(ct);
        return new CustomerOrdersResponse(orders.Select(MapToDto), total, q.Page, q.PageSize);
    }

    private static OrderDto MapToDto(Order o) =>
        new(o.Id, o.CustomerId, o.CustomerEmail, o.CustomerFirstName,
            o.Status.ToString(), o.SubTotal, o.DeliveryFee,
            o.TaxAmount, 0m, o.TotalAmount, o.DeliveryAddress, o.Notes, o.CreatedAt,
            o.EstimatedDelivery, o.DeliveredAt,
            o.Items.Select(i => new OrderItemDto(i.ProductId, i.ProductName, i.Quantity, i.UnitPrice, i.TotalPrice)).ToList());
}

/// <summary>
/// Query that computes real-time delivery statistics for the driver dashboard.
/// Aggregates counts across multiple order statuses to give drivers a quick
/// overview of their workload without navigating to individual status lists.
/// </summary>
public record GetDriverStatsQuery : IQuery<DriverStatsDto>;

/// <summary>
/// DTO carrying the four key metrics shown on the driver dashboard:
/// pending orders awaiting pickup, orders currently out for delivery,
/// deliveries completed today, and the all-time total delivered count.
/// </summary>
public record DriverStatsDto(int Pending, int OutForDelivery, int DeliveredToday, int TotalDelivered);

/// <summary>
/// Handles <see cref="GetDriverStatsQuery"/> by querying three status buckets
/// (Processing, OutForDelivery, Delivered) and computing the "delivered today"
/// count by filtering on <c>DeliveredAt.Date == today</c>.
/// </summary>
public class GetDriverStatsHandler(IOrderRepository repo)
    : IQueryHandler<GetDriverStatsQuery, DriverStatsDto>
{
    /// <inheritdoc/>
    public async Task<DriverStatsDto> Handle(GetDriverStatsQuery q, CancellationToken ct)
    {
        var processing = await repo.GetByStatusAsync(OrderStatus.Processing, ct);
        var outForDelivery = await repo.GetByStatusAsync(OrderStatus.OutForDelivery, ct);
        var delivered = await repo.GetByStatusAsync(OrderStatus.Delivered, ct);

        var today = DateTime.UtcNow.Date;
        var deliveredToday = delivered.Count(o => o.DeliveredAt.HasValue && o.DeliveredAt.Value.Date == today);

        return new DriverStatsDto(
            Pending: processing.Count(),
            OutForDelivery: outForDelivery.Count(),
            DeliveredToday: deliveredToday,
            TotalDelivered: delivered.Count()
        );
    }
}
