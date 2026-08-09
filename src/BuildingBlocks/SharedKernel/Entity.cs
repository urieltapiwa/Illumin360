namespace Illumin360.SharedKernel;

/// <summary>
/// Base class for domain entities with a strongly-identifiable key and the ability to
/// raise domain events (charter Part 5: Domain emits domain events).
/// </summary>
/// <typeparam name="TId">The identifier type.</typeparam>
public abstract class Entity<TId>
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>Initialises the entity with its identity.</summary>
    protected Entity(TId id) => Id = id;

    /// <summary>The entity's unique identity.</summary>
    public TId Id { get; protected init; }

    /// <summary>Domain events raised by this entity since it was loaded.</summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>Records a domain event to be dispatched after persistence.</summary>
    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    /// <summary>Clears recorded domain events (called after dispatch).</summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}

/// <summary>Marker for an in-process domain event.</summary>
public interface IDomainEvent
{
    /// <summary>When the event occurred (UTC).</summary>
    DateTimeOffset OccurredOn { get; }
}
