namespace NotificationService.Domain;

/// <summary>
/// An in-app notification persisted to the database and delivered to the user
/// via SignalR in real-time. Notifications are also shown in the bell-icon dropdown
/// in the Angular frontend.
/// </summary>
public class Notification
{
    /// <summary>Unique identifier.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>The user this notification belongs to.</summary>
    public Guid UserId { get; set; }

    /// <summary>Short notification title shown in the bell dropdown.</summary>
    public string Title { get; set; } = "";

    /// <summary>Full notification message body.</summary>
    public string Message { get; set; } = "";

    /// <summary>
    /// Visual type used by the frontend to pick an icon and colour.
    /// Valid values: "info", "success", "warning", "error", "order".
    /// </summary>
    public string Type { get; set; } = "info";

    /// <summary>Optional deep-link URL (e.g. "/orders") that the notification navigates to when clicked.</summary>
    public string? Link { get; set; }

    /// <summary>Whether the user has read this notification. Unread notifications increment the bell badge count.</summary>
    public bool IsRead { get; set; }

    /// <summary>UTC timestamp when the notification was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
