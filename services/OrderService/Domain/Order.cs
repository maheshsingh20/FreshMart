using SharedKernel.Domain;
using SharedKernel.Events;

namespace OrderService.Domain;

public enum OrderStatus
{
    Pending, PaymentPending, PaymentConfirmed, PaymentFailed,
    Processing, Shipped, OutForDelivery, Delivered, Cancelled, Refunded
}

public class Order : AggregateRoot
{
    public Guid CustomerId { get; private set; }
    public string CustomerEmail { get; private set; } = "";
    public string CustomerFirstName { get; private set; } = "";
    public OrderStatus Status { get; private set; } = OrderStatus.Pending;
    public decimal SubTotal { get; private set; }
    public decimal DeliveryFee { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public string DeliveryAddress { get; private set; } = default!;
    public string? Notes { get; private set; }
    public DateTime? EstimatedDelivery { get; private set; }
    public DateTime? DeliveredAt { get; private set; }
    public string? CancellationReason { get; private set; }

    private readonly List<OrderItem> _items = [];
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    private Order() { }

    public static Order Create(Guid customerId, string deliveryAddress,
        IEnumerable<(Guid ProductId, string Name, int Qty, decimal Price)> items,
        decimal deliveryFee = 2.99m, decimal taxRate = 0.08m, string? notes = null,
        string customerEmail = "", string customerFirstName = "")
    {
        var order = new Order
        {
            CustomerId = customerId,
            CustomerEmail = customerEmail,
            CustomerFirstName = customerFirstName,
            DeliveryAddress = deliveryAddress,
            DeliveryFee = deliveryFee,
            Notes = notes
        };

        foreach (var (productId, name, qty, price) in items)
            order._items.Add(OrderItem.Create(order.Id, productId, name, qty, price));

        order.SubTotal = order._items.Sum(i => i.TotalPrice);
        order.TaxAmount = Math.Round(order.SubTotal * taxRate, 2);
        order.TotalAmount = order.SubTotal + order.DeliveryFee + order.TaxAmount;

        order.AddDomainEvent(new OrderCreatedEvent(
            order.Id, order.CustomerId, order.TotalAmount,
            order._items.Select(i => new OrderItemDto(i.ProductId, i.ProductName, i.Quantity, i.UnitPrice)).ToList(),
            DateTime.UtcNow));

        return order;
    }

    public void ConfirmPayment()
    {
        Status = OrderStatus.PaymentConfirmed;
        SetUpdated();
    }

    public void FailPayment()
    {
        Status = OrderStatus.PaymentFailed;
        SetUpdated();
        AddDomainEvent(new OrderCancelledEvent(Id, "Payment failed", DateTime.UtcNow));
    }

    public void StartProcessing()
    {
        Status = OrderStatus.Processing;
        SetUpdated();
    }

    public void Ship() { Status = OrderStatus.Shipped; SetUpdated(); }
    public void OutForDelivery() { Status = OrderStatus.OutForDelivery; SetUpdated(); }

    public void Deliver()
    {
        Status = OrderStatus.Delivered;
        DeliveredAt = DateTime.UtcNow;
        SetUpdated();
    }

    public void Cancel(string reason)
    {
        if (Status is OrderStatus.Delivered or OrderStatus.Shipped)
            throw new InvalidOperationException("Cannot cancel a shipped or delivered order.");
        Status = OrderStatus.Cancelled;
        CancellationReason = reason;
        SetUpdated();
        AddDomainEvent(new OrderCancelledEvent(Id, reason, DateTime.UtcNow));
    }

    public void SetEstimatedDelivery(DateTime eta) { EstimatedDelivery = eta; SetUpdated(); }
}

public class OrderItem : Entity
{
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = default!;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal TotalPrice => UnitPrice * Quantity;

    private OrderItem() { }

    public static OrderItem Create(Guid orderId, Guid productId, string name, int qty, decimal price) =>
        new() { OrderId = orderId, ProductId = productId, ProductName = name, Quantity = qty, UnitPrice = price };
}
