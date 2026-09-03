namespace Hris.Foundation.JobProcessing.Domain;

/// <summary>
/// Repository contract for the <see cref="Job"/> Aggregate Root, per repositories.md's
/// "interface in the Domain layer... implementation in Infrastructure" split.
/// <see cref="ListByQueueAsync"/> backs job-processing.md's own Job History section
/// ("searchable and auditable"). <paramref name="tenantId"/> is mandatory on that
/// method for the same reason every other population-scale query in this codebase
/// requires it (<c>CTR-ISO-004</c>).
/// </summary>
public interface IJobRepository
{
    Task<Job?> GetByIdAsync(JobId id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Job>> ListByQueueAsync(JobQueueId jobQueueId, Guid tenantId, int maxResults, CancellationToken cancellationToken);

    Task AddAsync(Job job, CancellationToken cancellationToken);
}
