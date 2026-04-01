namespace UserService.Domain;

/// <summary>
/// A flat read-only projection of a user record from the AuthService database.
/// Used exclusively by the admin UserService for listing and managing users.
/// This is not an aggregate — it is a query model that maps directly to the Users table
/// in the shared GroceryAuth database.
/// </summary>
public class ManagedUser
{
    /// <summary>User's unique identifier (same as in AuthService).</summary>
    public Guid Id { get; set; }

    /// <summary>User's email address.</summary>
    public string Email { get; set; } = "";

    /// <summary>User's first name.</summary>
    public string FirstName { get; set; } = "";

    /// <summary>User's last name.</summary>
    public string LastName { get; set; } = "";

    /// <summary>User's role: "Customer", "Admin", "StoreManager", or "DeliveryDriver".</summary>
    public string Role { get; set; } = "Customer";

    /// <summary>Optional phone number.</summary>
    public string? PhoneNumber { get; set; }

    /// <summary>Whether the account is active. Deactivated users cannot log in.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>UTC timestamp when the account was created.</summary>
    public DateTime CreatedAt { get; set; }
}
