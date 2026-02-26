using SignalRDemo.Domain.Events;
using SignalRDemo.Domain.ValueObjects;

namespace SignalRDemo.Domain.Aggregates;

public abstract class AggregateRoot<TId> where TId : EntityId
{
    public TId Id { get; set; } = null!;
    public int Version { get; protected set; }

    private readonly List<DomainEvent> _domainEvents = new();
    public IReadOnlyList<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(DomainEvent @event)
    {
        _domainEvents.Add(@event);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    public abstract void Apply(DomainEvent @event);

    protected void ApplyToEntity(AggregateRoot<TId> entity, DomainEvent @event)
    {
        entity.Apply(@event);
    }
}
