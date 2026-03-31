using OrderService.Domain;
using SharedKernel.CQRS;

namespace OrderService.Application.Queries;

public record OrderDto(Guid Id, Guid CustomerId, string CustomerEmail, string CustomerFirstName,
    string Status, decimal SubTotal,
    decimal DeliveryFee, decimal TaxAmount, decimal DiscountAmount, decimal TotalAmount, string DeliveryAddress,
    string? Notes, DateTime CreatedAt, DateTime? EstimatedDelivery, DateTime? DeliveredAt,
    List<OrderItemDto> Items);

public record OrderItemDto(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice, decimal TotalPrice);

public record GetOrderByIdQuery(Guid OrderId) : IQuery<OrderDto?>;
public record GetCustomerOrdersQuery(Guid CustomerId, int Page = 1, int PageSize = 10) : IQuery<CustomerOrdersResponse>;
public record CustomerOrdersResponse(IEnumerable<OrderDto> Orders, int Total, int Page, int PageSize);

public class GetOrderByIdHandler(IOrderRepository repo) : IQueryHandler<GetOrderByIdQuery, OrderDto?>
{
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

public class GetCustomerOrdersHandler(IOrderRepository repo)
    : IQueryHandler<GetCustomerOrdersQuery, CustomerOrdersResponse>
{
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

public record GetOrdersByStatusQuery(string Status) : IQuery<IEnumerable<OrderDto>>;

public class GetOrdersByStatusHandler(IOrderRepository repo)
    : IQueryHandler<GetOrdersByStatusQuery, IEnumerable<OrderDto>>
{
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

// All orders for Admin/StoreManager
public record GetAllOrdersQuery(int Page = 1, int PageSize = 50) : IQuery<CustomerOrdersResponse>;

public class GetAllOrdersHandler(IOrderRepository repo)
    : IQueryHandler<GetAllOrdersQuery, CustomerOrdersResponse>
{
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

// Driver stats computed from live order data
public record GetDriverStatsQuery : IQuery<DriverStatsDto>;
public record DriverStatsDto(int Pending, int OutForDelivery, int DeliveredToday, int TotalDelivered);

public class GetDriverStatsHandler(IOrderRepository repo)
    : IQueryHandler<GetDriverStatsQuery, DriverStatsDto>
{
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
