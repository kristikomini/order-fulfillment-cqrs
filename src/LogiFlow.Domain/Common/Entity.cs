using MediatR;

namespace LogiFlow.Domain.Common;

/// <summary>
/// Base class for aggregate roots. Collects domain events raised while the
/// aggregate mutates; the persistence layer drains them into the outbox
/// inside the same transaction that saves the state change.
/// </summary>
public abstract class Entity
{
    private readonly List<IDomainEvent> _domainEvents = new();

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}

/// <summary>
/// Marker for domain events. Inherits MediatR's <see cref="INotification"/>
/// (from MediatR.Contracts — interfaces only, no runtime dependency) so the
/// outbox processor can publish them through the in-process mediator.
/// </summary>
public interface IDomainEvent : INotification
{
    DateTimeOffset OccurredAt { get; }
}
