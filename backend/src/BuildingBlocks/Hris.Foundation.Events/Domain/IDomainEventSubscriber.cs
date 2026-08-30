using Hris.SharedKernel;

namespace Hris.Foundation.Events.Domain;

/// <summary>
/// The contract a consumer implements to react to one kind of
/// <see cref="IDomainEvent"/>, per this framework's Event Subscription section
/// ("Consumers may subscribe by: Event Type...") -- named after that section's own
/// "consumer"/"subscribe" vocabulary rather than <c>IDomainEventHandler</c>, which
/// CA1711 reserves for a type built on the CLR's own delegate-based
/// <see cref="EventHandler"/> pattern; this is an unrelated, async-Task-returning
/// shape, closer to MediatR's own <c>INotificationHandler&lt;T&gt;</c>.
///
/// Framework-agnostic by design -- this does not depend on MediatR or any other
/// mediator package, per `CTR-ARC-001` -- an Infrastructure-layer dispatcher adapts
/// registered implementations of this interface to whatever delivery mechanism
/// (in-process today, a broker later) actually invokes them.
///
/// Per this framework's own Implementation Guidance ("Make every consumer
/// idempotent; delivery is at-least-once and redelivery will occur, `CTR-WFL-003`"),
/// an implementation must tolerate being called more than once for the same event
/// without duplicating its own effect.
/// </summary>
public interface IDomainEventSubscriber<in TEvent>
    where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken);
}
