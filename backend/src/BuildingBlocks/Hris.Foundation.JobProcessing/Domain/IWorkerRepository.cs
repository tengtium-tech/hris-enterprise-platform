namespace Hris.Foundation.JobProcessing.Domain;

/// <summary>
/// Repository contract for the <see cref="Worker"/> Aggregate Root, per
/// repositories.md's "interface in the Domain layer... implementation in
/// Infrastructure" split.
/// </summary>
public interface IWorkerRepository
{
    Task<Worker?> GetByIdAsync(WorkerId id, CancellationToken cancellationToken);

    Task AddAsync(Worker worker, CancellationToken cancellationToken);
}
