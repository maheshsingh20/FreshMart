namespace Backend.DTOs;

public record OrderItemDto(string ProductId, string ProductName, int Quantity, decimal UnitPrice, decimal TotalPrice);
public record OrderDto(string Id, string CustomerId, string Status, decimal SubTotal, decimal DeliveryFee, decimal TaxAmount, decimal DiscountAmount, decimal TotalAmount, string DeliveryAddress, string? Notes, string CreatedAt, string? EstimatedDelivery, string? DeliveredAt, IEnumerable<OrderItemDto> Items);
public record CreateOrderRequest(string DeliveryAddress, string? Notes, string? CouponCode);
public record UpdateOrderStatusRequest(string Status);
