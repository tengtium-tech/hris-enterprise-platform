namespace Hris.SharedKernel;

/// <summary>
/// Marker contract for every Domain Event in the platform: an immutable record of a
/// business fact that has already happened.
///
/// Grounded in docs/02-architecture/04-domain-driven-design/domain-events.md's Event
/// Structure section and Event Immutability ("Once created: Never modified, Never
/// reused, Never updated"). Only <c>EventId</c> and <c>OccurredOnUtc</c> are fixed
/// here -- the rest of Event Structure (Aggregate Id, business data, Correlation Id)
/// is naturally carried by each concrete event's own strongly typed payload rather
/// than forced into this shared shape, per shared-kernel.md's "remain intentionally
/// small" principle.
///
/// A concrete event is a <c>record</c> implementing this interface, e.g.
/// <c>public sealed record ConfigurationPublished(ConfigurationId ConfigurationId, ...)
/// : IDomainEvent</c>. Only an <see cref="AggregateRoot{TId}"/> may raise one
/// (domain-events.md, "Event Ownership": "Application Services do not invent
/// business events"). Dispatch (in-process today; outbox-backed once the Event
/// Framework and persistence exist) is an Infrastructure concern and never
/// referenced from here, per CTR-ARC-001.
/// </summary>
public interface IDomainEvent
{
    Guid EventId { get; }

    DateTimeOffset OccurredOnUtc { get; }
}
