namespace Hris.Foundation.StatutoryReferenceData.Domain;

/// <summary>
/// Every field statutory-reference-data.md's own Provenance section requires a
/// <see cref="StatutoryTableVersion"/> to record: "The issuing authority... the
/// issuance reference... the publication date... the date the platform verified the
/// transcription," plus the second-reviewer signoff Update Lifecycle Requirement 2
/// separately requires. A plain positional record, not a <c>ValueObject</c>-derived
/// type with its own <c>Create</c> factory -- the identical "plain record, persisted as
/// a single JSON column, no independent per-row query need of its own component parts"
/// choice <c>SearchFieldDefinition</c>'s own remarks make, and for the same reason: the
/// one query this framework's own <see cref="IStatutoryTableVersionRepository.GetEffectiveVersionAsync"/>
/// needs (has this version been signed off) is applied in memory over an already-small,
/// already-materialized candidate set (Selection Rule, below), never pushed into a SQL
/// <c>WHERE</c> clause against JSON internals.
///
/// <see cref="SignoffStatus"/>/<see cref="SignoffDateUtc"/>/<see cref="SignoffBy"/> start
/// null/<see cref="StatutorySignoffStatus.PendingHumanSignoff"/> at
/// <see cref="StatutoryTableVersion.Publish"/> and are only ever replaced, as a whole
/// new immutable record via <c>with</c>, by <see cref="StatutoryTableVersion.RecordSignoff"/>
/// -- never by editing the version's own substantive rate/bracket data, which
/// statutory-reference-data.md's own Update Lifecycle Requirement 1 states plainly is
/// never edited once published.
/// </summary>
public sealed record StatutoryTableProvenance(
    string IssuingAuthority,
    string IssuanceReference,
    DateTimeOffset PublicationDateUtc,
    StatutoryVerificationSourceType SourceType,
    DateTimeOffset ReadDateUtc,
    StatutorySignoffStatus SignoffStatus,
    DateTimeOffset? SignoffDateUtc,
    string? SignoffBy);
