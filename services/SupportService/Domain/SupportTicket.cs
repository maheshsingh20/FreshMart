namespace SupportService.Domain;

/// <summary>
/// A customer support ticket opened when a customer needs help with an order, payment, or product.
/// Tickets have a real-time chat thread via SignalR (<see cref="SupportMessage"/> collection).
/// </summary>
public class SupportTicket
{
    /// <summary>Unique identifier.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>The customer who opened the ticket.</summary>
    public Guid CustomerId { get; set; }

    /// <summary>Customer display name — denormalized to avoid calling AuthService on every ticket load.</summary>
    public string CustomerName { get; set; } = "";

    /// <summary>Customer email — used for email notifications on ticket updates.</summary>
    public string CustomerEmail { get; set; } = "";

    /// <summary>Short description of the issue.</summary>
    public string Subject { get; set; } = "";

    /// <summary>Issue category: "General", "Order", "Payment", "Delivery", or "Product".</summary>
    public string Category { get; set; } = "General";

    /// <summary>Current lifecycle status: "Open", "InProgress", "Resolved", or "Closed".</summary>
    public string Status { get; set; } = "Open";

    /// <summary>Urgency level: "Low", "Medium", or "High".</summary>
    public string Priority { get; set; } = "Medium";

    /// <summary>UTC timestamp when the ticket was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>UTC timestamp of the last status change or message.</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>UTC timestamp when the ticket was resolved. <c>null</c> if still open.</summary>
    public DateTime? ResolvedAt { get; set; }

    /// <summary>Ordered chat messages between the customer and support staff.</summary>
    public ICollection<SupportMessage> Messages { get; set; } = [];
}

/// <summary>
/// A single message in a <see cref="SupportTicket"/> chat thread.
/// Sent in real-time via SignalR and persisted to the database.
/// </summary>
public class SupportMessage
{
    /// <summary>Unique identifier.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>The ticket this message belongs to.</summary>
    public Guid TicketId { get; set; }

    /// <summary>User ID of the sender (customer or staff member).</summary>
    public Guid SenderId { get; set; }

    /// <summary>Display name of the sender.</summary>
    public string SenderName { get; set; } = "";

    /// <summary>Role of the sender: "Customer" or "Staff".</summary>
    public string SenderRole { get; set; } = "Customer";

    /// <summary>Message body text.</summary>
    public string Message { get; set; } = "";

    /// <summary><c>true</c> if the message was sent by a staff member; used to style messages differently in the UI.</summary>
    public bool IsStaff { get; set; }

    /// <summary>UTC timestamp when the message was sent.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
