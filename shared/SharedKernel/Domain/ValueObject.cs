namespace SharedKernel.Domain;

/// <summary>
/// Base class for value objects — immutable domain concepts identified by their values, not by identity.
/// Examples: Money, Address, EmailAddress.
/// Two value objects are equal if all their components are equal.
/// </summary>
public abstract class ValueObject
{
    /// <summary>
    /// Returns the ordered set of values that define equality for this value object.
    /// Subclasses must yield every field that participates in equality comparison.
    /// </summary>
    protected abstract IEnumerable<object> GetEqualityComponents();

    /// <summary>
    /// Compares two value objects by their equality components rather than by reference.
    /// </summary>
    /// <param name="obj">The object to compare against.</param>
    /// <returns><c>true</c> if both objects are of the same type and all components match.</returns>
    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType()) return false;
        return ((ValueObject)obj).GetEqualityComponents().SequenceEqual(GetEqualityComponents());
    }

    /// <summary>
    /// Computes a hash code from all equality components so value objects work correctly in hash-based collections.
    /// </summary>
    public override int GetHashCode() =>
        GetEqualityComponents().Aggregate(1, (current, obj) =>
            HashCode.Combine(current, obj?.GetHashCode() ?? 0));

    /// <summary>Equality operator that delegates to <see cref="Equals(object?)"/>.</summary>
    public static bool operator ==(ValueObject? left, ValueObject? right) =>
        left?.Equals(right) ?? right is null;

    /// <summary>Inequality operator that delegates to <see cref="Equals(object?)"/>.</summary>
    public static bool operator !=(ValueObject? left, ValueObject? right) => !(left == right);
}
