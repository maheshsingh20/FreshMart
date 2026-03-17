namespace OrderService.Domain;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<Order>> GetByCustomerAsync(Guid customerId, int page, int pageSize, CancellationToken ct = default);
    Task<IEnumerable<Order>> GetByStatusAsync(OrderStatus status, CancellationToken ct = default);
    Task<int> GetTotalByCustomerAsync(Guid customerId, CancellationToken ct = default);
    Task AddAsync(Order order, CancellationToken ct = default);
    Task UpdateAsync(Order order, CancellationToken ct = default);
}
