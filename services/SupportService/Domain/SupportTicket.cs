namespace SupportService.Domain;

public class SupportTicket
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = "";
    public string CustomerEmail { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Category { get; set; } = "General";
    public string Status { get; set; } = "Open";
    public string Priority { get; set; } = "Medium";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
    public ICollection<SupportMessage> Messages { get; set; } = [];
}

public class SupportMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TicketId { get; set; }
    public Guid SenderId { get; set; }
    public string SenderName { get; set; } = "";
    public string SenderRole { get; set; } = "Customer";
    public string Message { get; set; } = "";
    public bool IsStaff { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
