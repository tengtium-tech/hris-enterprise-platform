namespace Hris.Foundation.Scheduling.Domain;

/// <summary>
/// Repository contract for the <see cref="Schedule"/> Aggregate Root, per
/// repositories.md's "interface in the Domain layer... implementation in
/// Infrastructure" split.
/// </summary>
public interface IScheduleRepository
{
    Task<Schedule?> GetByIdAsync(ScheduleId id, CancellationToken cancellationToken);

    Task AddAsync(Schedule schedule, CancellationToken cancellationToken);
}
