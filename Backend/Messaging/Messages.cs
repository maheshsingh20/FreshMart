namespace Backend.Messaging;

// Published when an order is placed (both COD and Razorpay)
public class OrderPlacedMessage
{
    public Guid OrderId { get; set; }
    public string OrderRef { get; set; } = "";
    public Guid CustomerId { get; set; }
    public string CustomerEmail { get; set; } = "";
    public string CustomerFirstName { get; set; } = "";
    public decimal Total { get; set; }
    public List<OrderItemLine> Items { get; set; } = [];
    public bool IsPaid { get; set; }
}

// Published when order status changes
public class OrderStatusChangedMessage
{
    public Guid OrderId { get; set; }
    public string OrderRef { get; set; } = "";
    public Guid CustomerId { get; set; }
    public string CustomerEmail { get; set; } = "";
    public string CustomerFirstName { get; set; } = "";
    public string NewStatus { get; set; } = "";
}

// Published when stock drops to low / zero after an order
public class StockAlertMessage
{
    public string ProductName { get; set; } = "";
    public int RemainingStock { get; set; }
}

public class OrderItemLine
{
    public string ProductName { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
