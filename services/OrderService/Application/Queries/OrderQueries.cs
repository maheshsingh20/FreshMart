using OrderService.Domain;
using SharedKernel.CQRS;

namespace OrderService.Application.Queries;

public record OrderDto(Guid Id, Guid CustomerId, string Status, decimal SubTotal,
    decimal DeliveryFee, decimal TaxAmount, decimal TotalAmount, string DeliveryAddress,
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
        new(o.Id, o.CustomerId, o.Status.ToString(), o.SubTotal, o.DeliveryFee,
            o.TaxAmount, o.TotalAmount, o.DeliveryAddress, o.Notes, o.CreatedAt,
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
        new(o.Id, o.CustomerId, o.Status.ToString(), o.SubTotal, o.DeliveryFee,
            o.TaxAmount, o.TotalAmount, o.DeliveryAddress, o.Notes, o.CreatedAt,
            o.EstimatedDelivery, o.DeliveredAt,
            o.Items.Select(i => new OrderItemDto(i.ProductId, i.ProductName, i.Quantity, i.UnitPrice, i.TotalPrice)).ToList());
}
