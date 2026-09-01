using Hris.SharedKernel;

namespace Hris.Foundation.Events.Domain;

/// <summary>
/// Identity of the <see cref="OutboxEntry"/> Aggregate Root. Source:
/// docs/02-architecture/05-data-architecture/outbox-pattern.md's Outbox Table section
/// ("Event identifier... Aggregate identifier...") -- this is the outbox row's own
/// identity, distinct from <see cref="EventEnvelope.EventId"/>, which identifies the
/// underlying business event the row carries.
/// </summary>
public readonly record struct OutboxEntryId(Guid Value) : IStronglyTypedId;
