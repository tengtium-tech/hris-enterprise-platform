namespace Hris.Foundation.JobProcessing.Domain;

/// <summary>
/// Repository contract for the <see cref="JobQueue"/> Aggregate Root, per
/// repositories.md's "interface in the Domain layer... implementation in
/// Infrastructure" split.
/// </summary>
public interface IJobQueueRepository
{
    Task<JobQueue?> GetByIdAsync(JobQueueId id, CancellationToken cancellationToken);

    Task<JobQueue?> GetByNameAsync(JobQueueName name, CancellationToken cancellationToken);

    Task<bool> ExistsByNameAsync(JobQueueName name, CancellationToken cancellationToken);

    Task AddAsync(JobQueue jobQueue, CancellationToken cancellationToken);
}
