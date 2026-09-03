namespace Hris.Foundation.Scheduling.Domain;

/// <summary>
/// Repository contract for the <see cref="ScheduleExecution"/> Aggregate Root, per
/// repositories.md's "interface in the Domain layer... implementation in
/// Infrastructure" split. <see cref="ListByScheduleAsync"/> backs
/// scheduling-framework.md's own Schedule History section.
/// </summary>
public interface IScheduleExecutionRepository
{
    Task<ScheduleExecution?> GetByIdAsync(ScheduleExecutionId id, CancellationToken cancellationToken);

    Task<IReadOnlyList<ScheduleExecution>> ListByScheduleAsync(
        ScheduleId scheduleId, Guid tenantId, int maxResults, CancellationToken cancellationToken);

    Task AddAsync(ScheduleExecution execution, CancellationToken cancellationToken);
}
