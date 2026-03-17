namespace Backend.DTOs;

public record ReviewDto(string Id, string ProductId, string CustomerId, string CustomerName, int Rating, string Comment, string CreatedAt);
public record CreateReviewRequest(int Rating, string Comment);
public record CouponValidateRequest(string Code, decimal OrderAmount);
public record CouponValidateResponse(bool Valid, string? Message, string? DiscountType, decimal DiscountValue, decimal DiscountAmount);
