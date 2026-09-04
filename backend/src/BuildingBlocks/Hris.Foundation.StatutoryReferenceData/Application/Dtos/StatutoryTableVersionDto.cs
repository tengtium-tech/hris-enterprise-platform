namespace Hris.Foundation.StatutoryReferenceData.Application.Dtos;

/// <summary>
/// The read-side shape <c>GetEffectiveStatutoryTableVersionQuery</c>/
/// <c>ListStatutoryTableVersionHistoryQuery</c> return, per dto-design.md's own
/// convention. Flattens <c>StatutoryTableProvenance</c> onto this one DTO rather than
/// nesting a provenance DTO of its own -- the same "read-side DTOs are shaped for their
/// own consumer, not mirrored 1:1 to the Domain layer's own object graph" choice
/// mapping.md's own stated preference already justifies for every other framework's own
/// mapper.
/// </summary>
public sealed record StatutoryTableVersionDto(
    Guid StatutoryTableVersionId,
    Guid StatutoryProgramId,
    string VersionLabel,
    DateTimeOffset EffectiveFromUtc,
    DateTimeOffset? EffectiveToUtc,
    string IssuingAuthority,
    string IssuanceReference,
    DateTimeOffset PublicationDateUtc,
    string SourceType,
    DateTimeOffset ReadDateUtc,
    string SignoffStatus,
    DateTimeOffset? SignoffDateUtc,
    string? SignoffBy,
    string ScheduleData,
    DateTimeOffset PublishedAtUtc);
