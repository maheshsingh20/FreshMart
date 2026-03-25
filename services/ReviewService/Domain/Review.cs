namespace ReviewService.Domain;

public class Review
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = "";
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
