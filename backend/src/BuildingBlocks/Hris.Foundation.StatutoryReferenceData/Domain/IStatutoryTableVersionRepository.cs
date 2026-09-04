namespace Hris.Foundation.StatutoryReferenceData.Domain;

/// <summary>
/// Repository interface in the Domain layer, implementation in Infrastructure, per
/// repositories.md's own split.
/// </summary>
public interface IStatutoryTableVersionRepository
{
    Task<StatutoryTableVersion?> GetByIdAsync(StatutoryTableVersionId id, CancellationToken cancellationToken);

    Task<bool> ExistsByProgramAndVersionLabelAsync(
        StatutoryProgramId statutoryProgramId, StatutoryTableVersionLabel versionLabel, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the single version whose <see cref="StatutoryTableVersion.EffectiveFromUtc"/>
    /// is the latest one at or before <paramref name="periodUtc"/> -- statutory-reference-data.md's
    /// own Selection Rule: "Payroll computation selects the table version in force
    /// during the payroll period being computed, never the currently active version."
    /// Deliberately does not filter by signoff status here: the version in force for a
    /// period is determined by <see cref="StatutoryTableVersion.EffectiveFromUtc"/>
    /// alone, never by which version happens to be signed off -- silently returning an
    /// older, signed-off version instead would apply the wrong period's rates, exactly
    /// what the Selection Rule forbids. The caller (<c>GetEffectiveStatutoryTableVersionQuery</c>'s
    /// own handler) checks <see cref="StatutoryTableVersion.Provenance"/>'s own
    /// <see cref="StatutorySignoffStatus"/> against this specific returned version and
    /// fails explicitly (Availability Requirement: "Absence of an applicable table for
    /// the period being computed is a hard failure, not a warning, and not a fallback to
    /// a default") if it is not yet signed off, rather than this method silently
    /// searching further back for one that is.
    /// </summary>
    Task<StatutoryTableVersion?> GetLatestEffectiveAsOfAsync(
        StatutoryProgramId statutoryProgramId, DateTimeOffset periodUtc, CancellationToken cancellationToken);

    /// <summary>
    /// Full version history for one program, most recent <see cref="StatutoryTableVersion.EffectiveFromUtc"/>
    /// first -- the audit/browsing query every other Sprint 4 framework's own
    /// "ListXHistoryAsync" repository method already establishes
    /// (<c>IJobRepository.ListByQueueAsync</c>, <c>IScheduleExecutionRepository</c>'s
    /// own history query).
    /// </summary>
    Task<IReadOnlyList<StatutoryTableVersion>> ListByProgramAsync(
        StatutoryProgramId statutoryProgramId, CancellationToken cancellationToken);

    Task AddAsync(StatutoryTableVersion version, CancellationToken cancellationToken);
}
