using Hris.Foundation.Events.Application.Dtos;
using Hris.Foundation.Events.Domain;

namespace Hris.Foundation.Events.Application.Mapping;

/// <summary>
/// Maps <see cref="OutboxEntry"/> to its query-side DTO, by hand rather than through a
/// registered Mapster profile -- the identical deviation <c>ConfigurationMapper</c> and
/// <c>IdentityMapper</c> state and justify for the same reason: every field here either
/// unwraps the owned <see cref="EventEnvelope"/> or converts an enum to its DTO-side
/// string.
/// </summary>
internal static class EventMapper
{
    public static OutboxEntryDto ToDto(this OutboxEntry entry) => new(
        entry.Id.Value,
        entry.Envelope.EventId,
        entry.Envelope.EventType,
        entry.Envelope.EventVersion,
        entry.Status.ToString(),
        entry.Envelope.OccurredOnUtc,
        entry.CreatedAtUtc,
        entry.DispatchedAtUtc,
        entry.AttemptCount,
        entry.LastAttemptAtUtc,
        entry.LastFailureReason,
        entry.Envelope.SourceModule,
        entry.Envelope.CorrelationId.Value,
        entry.Envelope.TenantId,
        entry.Envelope.Payload);
}
