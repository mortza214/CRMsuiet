using CrmSuite.BuildingBlocks.Domain.Abstractions;

namespace CrmSuite.BuildingBlocks.Domain.Entities;

/// <summary>An aggregate root is the only entry point for changing a consistency boundary.</summary>
public abstract class AggregateRoot<TId> : Entity<TId>, IHasDomainEvents
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents() => _domainEvents.Clear();
}

/// <summary>Convenience aggregate root for <see cref="Guid"/>-keyed aggregates.</summary>
public abstract class AggregateRoot : AggregateRoot<Guid>
{
}
