namespace Hris.Foundation.Events.Application.Dtos;

/// <summary>
/// The read-side shape of an <see cref="Domain.OutboxEntry"/>, for Dead Letter Queue
/// Monitoring and Diagnostics (event-framework.md's Dead Letter Queue section) -- the
/// identical primitive-only reasoning <c>ConfigurationSettingDto</c>/<c>UserAccountDto</c>
/// already state for their own query-side DTOs. Never carries the raw
/// <c>EventEnvelope.Payload</c> string as anything richer than the opaque
/// already-serialized text it is -- deserializing it into a strongly typed shape here
/// would require the same event-type registry this Sprint's dispatcher deliberately
/// does not yet build (see <c>OutboxDispatcherBackgroundService</c>'s own remarks).
/// </summary>
public sealed record OutboxEntryDto(
    Guid Id,
    Guid EventId,
    string EventType,
    int EventVersion,
    string Status,
    DateTimeOffset OccurredOnUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? DispatchedAtUtc,
    int AttemptCount,
    DateTimeOffset? LastAttemptAtUtc,
    string? LastFailureReason,
    string SourceModule,
    Guid CorrelationId,
    Guid? TenantId,
    string Payload);
