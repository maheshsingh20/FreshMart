using MediatR;

namespace SharedKernel.Domain;

/// <summary>
/// Marker interface for all domain events raised within an aggregate.
/// Implements <see cref="INotification"/> so MediatR can dispatch them to in-process handlers.
/// Domain events are collected on the entity and dispatched after the DB transaction commits.
/// </summary>
public interface IDomainEvent : INotification
{
    /// <summary>Unique identifier for this specific event occurrence, used for idempotency checks.</summary>
    Guid EventId { get; }

    /// <summary>UTC timestamp of when the domain event was raised inside the aggregate.</summary>
    DateTime OccurredOn { get; }

    /// <summary>Discriminator string used for routing and deserialization across service boundaries.</summary>
    string EventType { get; }
}

/// <summary>
/// Base record for all concrete domain events.
/// Provides auto-generated <see cref="EventId"/> and <see cref="OccurredOn"/> so subclasses
/// only need to declare their payload and override <see cref="EventType"/>.
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    /// <inheritdoc/>
    public Guid EventId { get; } = Guid.NewGuid();

    /// <inheritdoc/>
    public DateTime OccurredOn { get; } = DateTime.UtcNow;

    /// <inheritdoc/>
    public abstract string EventType { get; }
}
