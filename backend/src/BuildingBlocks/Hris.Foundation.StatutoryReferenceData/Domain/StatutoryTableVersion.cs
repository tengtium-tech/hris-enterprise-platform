using System.Text.Json;
using Hris.SharedKernel;

namespace Hris.Foundation.StatutoryReferenceData.Domain;

/// <summary>
/// Aggregate Root for one immutable-once-published version of a
/// <see cref="StatutoryProgram"/>'s own table -- statutory-reference-data.md's own
/// Update Lifecycle Requirement 1: "A new version is added; existing versions are never
/// edited. Historical computations must remain reproducible." Population-scale sibling
/// to <see cref="StatutoryProgram"/>, the same config-aggregate-plus-occurrence-aggregate
/// split <c>NumberSeries</c>/<c>IssuedNumber</c>, <c>SearchIndexDefinition</c>/
/// <c>IndexedDocument</c>, <c>Schedule</c>/<c>ScheduleExecution</c>, and
/// <c>JobQueue</c>/<c>Job</c> each already establish.
///
/// <see cref="ScheduleData"/> is stored as an opaque JSON string, validated only for
/// syntactic well-formedness at <see cref="Publish"/> time, never for a canonical
/// bracket/schedule shape -- statutory-reference-data/sss-2025-01.yaml's own MSC/EC/MPF
/// bracket structure, philhealth's own percentage-of-compensation structure, pagibig's
/// own percentage-with-ceiling structure, and bir's own progressive-tax-bracket
/// structure are four genuinely different shapes, and this Sprint's own scope (this
/// framework's own AI Implementation Guidance: "Treat statutory tables as
/// platform-provided reference data... Preserve provenance metadata with every table
/// version") is recording and effective-dated selection, not computation -- inventing
/// one canonical bracket schema here would be exactly the "manufacture a decision point
/// the specification does not give" CLAUDE.md's own Working Principles warn against.
/// Payroll module (a later Phase, not yet built) is this field's own real consumer and
/// owns interpreting it.
///
/// <see cref="EffectiveToUtc"/> is set once at <see cref="Publish"/> time (almost always
/// left <c>null</c>, matching every one of the platform's own four shipped fixture
/// files -- statutory-reference-data/README.md: "None of these files carries an
/// effective_to date... A file is superseded by adding a new version file, never by
/// editing this one") and never changed afterward by any method on this aggregate --
/// there is deliberately no "Supersede" method that would backfill it onto an earlier
/// version when a newer one publishes, since doing so would edit an already-published
/// version, exactly what Requirement 1 forbids.
/// <see cref="IStatutoryTableVersionRepository.GetEffectiveVersionAsync"/> determines
/// which version is in force for a given period entirely by comparing
/// <see cref="EffectiveFromUtc"/> across a program's own versions (Selection Rule:
/// "Payroll computation selects the table version in force during the payroll period
/// being computed, never the currently active version"), never by consulting
/// <see cref="EffectiveToUtc"/>.
/// </summary>
public sealed class StatutoryTableVersion : AggregateRoot<StatutoryTableVersionId>
{
    public StatutoryProgramId StatutoryProgramId { get; }

    public StatutoryTableVersionLabel VersionLabel { get; }

    public DateTimeOffset EffectiveFromUtc { get; }

    public DateTimeOffset? EffectiveToUtc { get; }

    public StatutoryTableProvenance Provenance { get; private set; }

    public string ScheduleData { get; }

    public DateTimeOffset PublishedAtUtc { get; }

    private StatutoryTableVersion(
        StatutoryTableVersionId id,
        StatutoryProgramId statutoryProgramId,
        StatutoryTableVersionLabel versionLabel,
        DateTimeOffset effectiveFromUtc,
        DateTimeOffset? effectiveToUtc,
        StatutoryTableProvenance provenance,
        string scheduleData,
        DateTimeOffset publishedAtUtc)
        : base(id)
    {
        StatutoryProgramId = statutoryProgramId;
        VersionLabel = versionLabel;
        EffectiveFromUtc = effectiveFromUtc;
        EffectiveToUtc = effectiveToUtc;
        Provenance = provenance;
        ScheduleData = scheduleData;
        PublishedAtUtc = publishedAtUtc;
    }

    /// <summary>
    /// Publishes a new table version. Uniqueness of <paramref name="versionLabel"/>
    /// within <paramref name="statutoryProgramId"/> is checked by the caller before this
    /// factory runs (<see cref="IStatutoryTableVersionRepository.ExistsByProgramAndVersionLabelAsync"/>),
    /// the same split <see cref="StatutoryProgram.Register"/>'s own remarks state.
    ///
    /// Every constructor parameter above shares its name with the property it sets
    /// (<c>publishedAtUtc</c> -&gt; <see cref="PublishedAtUtc"/>, not a differently-named
    /// <c>nowUtc</c>) -- the proactive naming discipline Scheduling and Job Processing
    /// Frameworks' own <c>Schedule</c>/<c>Job</c> remarks already establish, avoiding the
    /// constructor-binding pitfall Search Framework's own <c>IndexedDocument</c>/
    /// <c>SearchExecution</c>/<c>SavedSearch</c> each needed a second constructor for.
    ///
    /// <paramref name="effectiveFromUtc"/> may be in the future relative to
    /// <paramref name="publishedAtUtc"/>, deliberately unchecked here -- Update Lifecycle
    /// Requirement 3: "A table version may be published ahead of its effective date, so
    /// that a known future change is in place before it applies."
    /// </summary>
    public static Result<StatutoryTableVersion> Publish(
        StatutoryProgramId statutoryProgramId,
        StatutoryTableVersionLabel versionLabel,
        DateTimeOffset effectiveFromUtc,
        DateTimeOffset? effectiveToUtc,
        StatutoryTableProvenance provenance,
        string? scheduleData,
        DateTimeOffset publishedAtUtc)
    {
        Guard.AgainstDefault(statutoryProgramId.Value, nameof(statutoryProgramId));
        Guard.AgainstNull(versionLabel, nameof(versionLabel));
        Guard.AgainstNull(provenance, nameof(provenance));

        if (effectiveToUtc is not null && effectiveToUtc < effectiveFromUtc)
        {
            return Result.Failure<StatutoryTableVersion>(StatutoryReferenceDataErrors.EffectiveToBeforeEffectiveFrom);
        }

        if (string.IsNullOrWhiteSpace(provenance.IssuingAuthority))
        {
            return Result.Failure<StatutoryTableVersion>(StatutoryReferenceDataErrors.IssuingAuthorityRequired);
        }

        if (string.IsNullOrWhiteSpace(provenance.IssuanceReference))
        {
            return Result.Failure<StatutoryTableVersion>(StatutoryReferenceDataErrors.IssuanceReferenceRequired);
        }

        var scheduleDataValidation = ValidateScheduleData(scheduleData);
        if (scheduleDataValidation.IsFailure)
        {
            return Result.Failure<StatutoryTableVersion>(scheduleDataValidation.Error);
        }

        var version = new StatutoryTableVersion(
            new StatutoryTableVersionId(Guid.NewGuid()),
            statutoryProgramId,
            versionLabel,
            effectiveFromUtc,
            effectiveToUtc,
            provenance,
            scheduleData!.Trim(),
            publishedAtUtc);

        version.AddDomainEvent(new StatutoryTableVersionPublished(
            Guid.NewGuid(), publishedAtUtc, version.Id, statutoryProgramId, versionLabel.Value, effectiveFromUtc));

        return Result.Success(version);
    }

    /// <summary>
    /// Records the second-reviewer confirmation Update Lifecycle Requirement 2 requires
    /// before this version may be trusted for production computation
    /// (statutory-reference-data/README.md's own Verification Status table:
    /// "pending-human-signoff... not yet the second-reviewer confirmation"). This is the
    /// one field group this aggregate ever changes after <see cref="Publish"/> --
    /// <see cref="Provenance"/>'s own substantive rate/bracket data
    /// (<see cref="ScheduleData"/>) is never touched, so this does not violate
    /// Requirement 1's "existing versions are never edited" for the table's own values,
    /// only backfills the review metadata the same requirement's own second sentence
    /// (Requirement 2) separately calls for. Refuses a second call --
    /// statutory-reference-data.md names one signoff event per version, not a
    /// re-signable one.
    /// </summary>
    public Result RecordSignoff(DateTimeOffset signoffDateUtc, string? signoffBy)
    {
        if (Provenance.SignoffStatus == StatutorySignoffStatus.SignedOff)
        {
            return Result.Failure(StatutoryReferenceDataErrors.AlreadySignedOff);
        }

        if (string.IsNullOrWhiteSpace(signoffBy))
        {
            return Result.Failure(StatutoryReferenceDataErrors.SignoffByRequired);
        }

        Provenance = Provenance with
        {
            SignoffStatus = StatutorySignoffStatus.SignedOff,
            SignoffDateUtc = signoffDateUtc,
            SignoffBy = signoffBy.Trim(),
        };

        AddDomainEvent(new StatutoryTableVersionSignedOff(Guid.NewGuid(), signoffDateUtc, Id, signoffDateUtc, signoffBy.Trim()));
        return Result.Success();
    }

    /// <summary>
    /// A Guard-clause-level technical precondition (syntactic JSON validity), not a
    /// business rule -- per guard-clauses.md, this framework never validates the table's
    /// own internal bracket/schedule shape, only that it is well-formed JSON at all.
    /// </summary>
    private static Result ValidateScheduleData(string? scheduleData)
    {
        if (string.IsNullOrWhiteSpace(scheduleData))
        {
            return Result.Failure(StatutoryReferenceDataErrors.ScheduleDataRequired);
        }

        try
        {
            using var _ = JsonDocument.Parse(scheduleData);
            return Result.Success();
        }
        catch (JsonException)
        {
            return Result.Failure(StatutoryReferenceDataErrors.ScheduleDataMustBeValidJson);
        }
    }
}
