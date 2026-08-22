namespace Payflow.Shared.Kernel;

/// <summary>
/// An <see cref="Entity{TId}"/> that is the consistency boundary for a cluster of objects and the
/// only member of that cluster external code is allowed to hold a reference to. Records domain
/// events raised while enforcing invariants so the Application layer can dispatch them after the
/// unit of work commits.
/// </summary>
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected AggregateRoot(TId id) : base(id) { }

    protected AggregateRoot() { }

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
