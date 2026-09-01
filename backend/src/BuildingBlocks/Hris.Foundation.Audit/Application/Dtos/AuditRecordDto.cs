namespace Hris.Foundation.Audit.Application.Dtos;

/// <summary>
/// The read-side shape of an <see cref="Domain.AuditRecord"/>, per the identical
/// primitive-only reasoning every other Sprint 3 framework's own query-side DTOs
/// state. <see cref="PreviousValue"/>/<see cref="NewValue"/> pass through as the
/// already-opaque, already-serialized strings <see cref="Domain.AuditRecord"/> itself
/// stores them as -- see that type's own remarks for why they are not richly typed
/// here either.
/// </summary>
public sealed record AuditRecordDto(
    Guid Id,
    DateTimeOffset TimestampUtc,
    Guid? ActorId,
    string Category,
    string Action,
    string BusinessEntity,
    string EntityIdentifier,
    string? PreviousValue,
    string? NewValue,
    string SourceSystem,
    string? ClientApplication,
    string? IpAddress,
    string? DeviceInformation,
    Guid? CorrelationId,
    string Outcome,
    IReadOnlyDictionary<string, string> Metadata);
