namespace Hris.SharedKernel;

/// <summary>
/// Base type for every Aggregate Root in the platform.
///
/// Grounded in docs/02-architecture/04-domain-driven-design/aggregate-design-rules.md
/// Rule 2 (one Aggregate Root per Aggregate; external components interact only with
/// it) and Rule 11 (important business changes raise Domain Events). Optimistic
/// concurrency (Rule 15's "Optimistic Concurrency" section) is deliberately not
/// represented here -- that document states plainly that the mechanism "belongs to
/// the infrastructure," and no persistence layer exists yet for any Sprint 3
/// Foundation framework (see backend/README.md); adding a version token to this base
/// type now would invent a shape ahead of the Infrastructure layer that will actually
/// need it.
///
/// <see cref="AddDomainEvent"/> is the only way a Domain Event enters
/// <see cref="DomainEvents"/> (domain-events.md, "Event Ownership": only Aggregates
/// raise events) and must be called only after the state change it describes has
/// already succeeded (domain-events.md, "Event Timing": "Never publish events for
/// failed operations"; invariants.md: "Domain Events should be published only after
/// all invariants have been satisfied"). Named <c>Add</c> rather than the docs' own
/// prose "Raise" verb because a method named <c>RaiseXxx</c> reads, to a Roslyn
/// analyzer (CA1030), as a candidate for the C# <c>event</c> keyword -- the wrong
/// mechanism here, since dispatch is deferred until after commit (ADR-0004,
/// `CTR-NTF-003`), not synchronous like a CLR event.
/// </summary>
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : IStronglyTypedId
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected AggregateRoot(TId id)
        : base(id)
    {
    }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
