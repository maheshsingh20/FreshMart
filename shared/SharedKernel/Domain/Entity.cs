namespace SharedKernel.Domain;

/// <summary>
/// Base class for all domain entities across all microservices.
///
/// Every entity gets:
///   - A unique Guid Id (generated on creation, never changes)
///   - CreatedAt / UpdatedAt timestamps (UTC)
///   - A domain events collection for publishing side-effects via RabbitMQ
///
/// Domain events are raised inside entity methods (e.g. User.Create raises UserRegisteredEvent).
/// They are collected here and dispatched after the DB transaction commits.
/// </summary>
public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; protected set; }

    // Internal list — only the entity itself can add events
    private readonly List<IDomainEvent> _domainEvents = [];

    // Exposed as read-only so handlers can read but not modify
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    // Called inside entity methods to record that something happened
    protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    // Called after events are dispatched to prevent double-publishing
    public void ClearDomainEvents() => _domainEvents.Clear();

    // Call this in any method that mutates state so UpdatedAt stays accurate
    protected void SetUpdated() => UpdatedAt = DateTime.UtcNow;
}

/// <summary>
/// Marker class for aggregate roots — the entry point for a consistency boundary.
/// Only aggregate roots should be loaded from the repository directly.
/// Child entities are accessed through the aggregate root.
/// </summary>
public abstract class AggregateRoot : Entity { }

