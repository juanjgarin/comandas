namespace Comandas.SharedKernel.Domain;

/// <summary>
/// The entry point of a consistency boundary: outside code talks to the aggregate root only,
/// and the root guarantees the invariants of everything inside it.
/// It also collects the domain events raised during an operation, to be dispatched after saving.
/// </summary>
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected AggregateRoot(TId id)
        : base(id)
    {
    }

    // Parameterless constructor for EF Core materialization only.
    protected AggregateRoot()
    {
    }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void ClearDomainEvents() => _domainEvents.Clear();

    protected void Raise(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }
}
