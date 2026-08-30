using Hris.SharedKernel;

namespace Hris.Foundation.Events.Domain;

/// <summary>
/// The single contract every other framework and, eventually, module depends on to
/// get its own raised <see cref="IDomainEvent"/>s dispatched, per this framework's
/// Transactional Publication section: "The event is written to an outbox table
/// within the same database transaction as the business change."
///
/// Notably, an <see cref="AggregateRoot{TId}"/> never calls this interface itself --
/// doing so from inside an aggregate would be exactly the Infrastructure access
/// domain-services.md and CTR-ARC-001 prohibit in the Domain layer. The intended
/// caller is the Application layer's own transaction/Unit-of-Work boundary, once one
/// exists: after a business change and its aggregate's accumulated
/// <see cref="AggregateRoot{TId}.DomainEvents"/> both commit successfully, that
/// boundary calls <see cref="PublishAsync"/> with the same events, inside the same
/// transaction, satisfying Requirement 1 below. No Infrastructure implementation
/// (the actual outbox table, or a dispatcher reading it) exists yet -- no EF Core
/// model exists for any Sprint 3 framework (backend/README.md).
///
/// This framework's stated Requirements 1-6:
/// 1. Domain events from a business change are written to the outbox within that
///    change's own transaction.
/// 2. Dispatch occurs after commit, never before.
/// 3. Dispatch is at-least-once; consumers (<see cref="IDomainEventSubscriber{TEvent}"/>)
///    are therefore idempotent.
/// 4. A committed business action cannot lose its event, including across process
///    restart (`CTR-NTF-003`).
/// 5. Failure to publish never rolls back a committed business action (`CTR-NTF-002`).
/// 6. Undispatched outbox entries survive process termination and are dispatched on
///    restart.
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync(IReadOnlyCollection<EventEnvelope> envelopes, CancellationToken cancellationToken);
}
