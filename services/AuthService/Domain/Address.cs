using SharedKernel.Domain;

namespace AuthService.Domain;

public class Address : Entity
{
    public Guid UserId { get; private set; }
    public string Label { get; private set; } = "";       // Home, Work, etc.
    public string Line1 { get; private set; } = "";       // Street address
    public string? Line2 { get; private set; }            // Apt, Floor, etc.
    public string City { get; private set; } = "";
    public string State { get; private set; } = "";
    public string Pincode { get; private set; } = "";
    public string Country { get; private set; } = "India";
    public bool IsDefault { get; private set; }

    private Address() { }

    public static Address Create(Guid userId, string label, string line1, string? line2,
        string city, string state, string pincode, string country, bool isDefault) =>
        new()
        {
            UserId = userId, Label = label, Line1 = line1, Line2 = line2,
            City = city, State = state, Pincode = pincode,
            Country = string.IsNullOrWhiteSpace(country) ? "India" : country,
            IsDefault = isDefault
        };

    public void Update(string label, string line1, string? line2,
        string city, string state, string pincode, string country)
    {
        Label = label; Line1 = line1; Line2 = line2;
        City = city; State = state; Pincode = pincode;
        Country = string.IsNullOrWhiteSpace(country) ? "India" : country;
        SetUpdated();
    }

    public void SetDefault(bool value) { IsDefault = value; SetUpdated(); }
}
