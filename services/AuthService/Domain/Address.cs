using SharedKernel.Domain;

namespace AuthService.Domain;

/// <summary>
/// Represents a saved delivery address belonging to a user.
/// Users can have multiple addresses; exactly one can be marked as default.
/// </summary>
public class Address : Entity
{
    /// <summary>The user this address belongs to.</summary>
    public Guid UserId { get; private set; }

    /// <summary>User-defined label for quick identification (e.g. "Home", "Work", "Parents").</summary>
    public string Label { get; private set; } = "";

    /// <summary>Primary street address line.</summary>
    public string Line1 { get; private set; } = "";

    /// <summary>Optional secondary line for apartment, floor, or landmark details.</summary>
    public string? Line2 { get; private set; }

    /// <summary>City or town name.</summary>
    public string City { get; private set; } = "";

    /// <summary>State or province.</summary>
    public string State { get; private set; } = "";

    /// <summary>Postal / PIN code used for delivery routing.</summary>
    public string Pincode { get; private set; } = "";

    /// <summary>Country name; defaults to "India" when not specified.</summary>
    public string Country { get; private set; } = "India";

    /// <summary>
    /// Whether this is the user's default delivery address.
    /// Only one address per user should have this set to <c>true</c> at any time.
    /// </summary>
    public bool IsDefault { get; private set; }

    /// <summary>Required by EF Core — not for direct use.</summary>
    private Address() { }

    /// <summary>
    /// Creates a new address for the given user.
    /// </summary>
    /// <param name="userId">Owner of the address.</param>
    /// <param name="label">Display label (e.g. "Home").</param>
    /// <param name="line1">Primary street line.</param>
    /// <param name="line2">Optional secondary line.</param>
    /// <param name="city">City name.</param>
    /// <param name="state">State name.</param>
    /// <param name="pincode">Postal code.</param>
    /// <param name="country">Country; falls back to "India" if blank.</param>
    /// <param name="isDefault">Whether to set this as the default address.</param>
    public static Address Create(Guid userId, string label, string line1, string? line2,
        string city, string state, string pincode, string country, bool isDefault) =>
        new()
        {
            UserId = userId, Label = label, Line1 = line1, Line2 = line2,
            City = city, State = state, Pincode = pincode,
            Country = string.IsNullOrWhiteSpace(country) ? "India" : country,
            IsDefault = isDefault
        };

    /// <summary>
    /// Updates all mutable fields of the address.
    /// </summary>
    /// <param name="label">New label.</param>
    /// <param name="line1">New primary street line.</param>
    /// <param name="line2">New optional secondary line.</param>
    /// <param name="city">New city.</param>
    /// <param name="state">New state.</param>
    /// <param name="pincode">New postal code.</param>
    /// <param name="country">New country; falls back to "India" if blank.</param>
    public void Update(string label, string line1, string? line2,
        string city, string state, string pincode, string country)
    {
        Label = label; Line1 = line1; Line2 = line2;
        City = city; State = state; Pincode = pincode;
        Country = string.IsNullOrWhiteSpace(country) ? "India" : country;
        SetUpdated();
    }

    /// <summary>
    /// Sets or clears the default flag on this address.
    /// Callers are responsible for clearing the flag on other addresses first.
    /// </summary>
    /// <param name="value"><c>true</c> to make this the default; <c>false</c> to unset it.</param>
    public void SetDefault(bool value) { IsDefault = value; SetUpdated(); }
}