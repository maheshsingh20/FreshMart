namespace Backend.Models;

public class SupportTicket
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = "";
    public string CustomerEmail { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Category { get; set; } = "Other"; // Order, Payment, Delivery, Product, Other
    public string Status { get; set; } = "Open";    // Open, InProgress, Resolved, Closed
    public string Priority { get; set; } = "Medium"; // Low, Medium, High
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }

    public AppUser? Customer { get; set; }
    public ICollection<SupportMessage> Messages { get; set; } = [];
}
