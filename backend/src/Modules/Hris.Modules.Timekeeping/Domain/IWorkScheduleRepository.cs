namespace Hris.Modules.Timekeeping.Domain;

/// <summary>
/// Repository contract for the <see cref="WorkSchedule"/> Aggregate Root. There is
/// deliberately no repository for <see cref="ScheduleAssignment"/> — it is reached
/// through its schedule (CTR-ARC-004).
/// </summary>
public interface IWorkScheduleRepository
{
    Task<WorkSchedule?> GetByIdAsync(WorkScheduleId id, CancellationToken cancellationToken);

    Task<IReadOnlyList<WorkSchedule>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken);

    /// <summary>
    /// Every version in one lineage. TK-002 resolution needs the full history, not
    /// only the current version, so a query about a past date can select the version
    /// that was in force then.
    /// </summary>
    Task<IReadOnlyList<WorkSchedule>> ListByLineageAsync(Guid lineageId, CancellationToken cancellationToken);

    Task AddAsync(WorkSchedule schedule, CancellationToken cancellationToken);
}
