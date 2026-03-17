namespace Backend.DTOs;

public record CartItemDto(string ProductId, string ProductName, decimal UnitPrice, string ImageUrl, int Quantity, decimal TotalPrice, decimal DiscountPercent, decimal OriginalPrice);
public record CartDto(string CustomerId, IEnumerable<CartItemDto> Items, decimal? BudgetLimit, string LastUpdated, decimal SubTotal, bool IsOverBudget, int TotalItems);
public record AddToCartRequest(Guid ProductId, int Quantity);
public record UpdateCartItemRequest(int Quantity);
public record SetBudgetRequest(decimal? BudgetLimit);
