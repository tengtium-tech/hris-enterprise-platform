using Hris.SharedKernel;

namespace Hris.Foundation.StatutoryReferenceData.Domain;

/// <summary>
/// statutory-reference-data.md does not carry a dedicated "Domain Events" section the
/// way scheduling-framework.md or job-processing.md do -- it is written in a business/
/// policy voice, not a technical domain-model spec. The two events below are grounded
/// directly in prose the document does state rather than invented ahead of a section
/// that does not exist: Update Lifecycle Requirement 6 ("Publication of a statutory
/// table version is an audited event") for <see cref="StatutoryTableVersionPublished"/>,
/// and Security Considerations ("Every change is audited, recording the actor, version,
/// and issuance reference") for <see cref="StatutoryTableVersionSignedOff"/> -- the only
/// other change this Aggregate Root's own shape permits post-publish.
/// <see cref="StatutoryProgram.Register"/> raises no event of its own, the identical
/// "the document names no registration event for this config aggregate" reasoning
/// <c>JobQueue.Register</c>'s own remarks already state for itself.
/// </summary>
public sealed record StatutoryTableVersionPublished(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    StatutoryTableVersionId StatutoryTableVersionId,
    StatutoryProgramId StatutoryProgramId,
    string VersionLabel,
    DateTimeOffset EffectiveFromUtc) : IDomainEvent;

public sealed record StatutoryTableVersionSignedOff(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    StatutoryTableVersionId StatutoryTableVersionId,
    DateTimeOffset SignoffDateUtc,
    string SignoffBy) : IDomainEvent;
