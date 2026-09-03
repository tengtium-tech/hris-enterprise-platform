using Hris.Foundation.JobProcessing.Domain;
using Hris.Infrastructure.Persistence;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hris.Foundation.JobProcessing.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IJobQueueRepository"/>, per repositories.md's
/// "interface in the Domain layer... implementation in Infrastructure" split.
/// </summary>
internal sealed class JobQueueRepository : IJobQueueRepository
{
    private readonly HrisDbContext _dbContext;

    public JobQueueRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public Task<JobQueue?> GetByIdAsync(JobQueueId id, CancellationToken cancellationToken) =>
        _dbContext.Set<JobQueue>().FirstOrDefaultAsync(jobQueue => jobQueue.Id == id, cancellationToken);

    public Task<JobQueue?> GetByNameAsync(JobQueueName name, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(name, nameof(name));

        return _dbContext.Set<JobQueue>().FirstOrDefaultAsync(jobQueue => jobQueue.Name == name, cancellationToken);
    }

    public Task<bool> ExistsByNameAsync(JobQueueName name, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(name, nameof(name));

        return _dbContext.Set<JobQueue>().AnyAsync(jobQueue => jobQueue.Name == name, cancellationToken);
    }

    public async Task AddAsync(JobQueue jobQueue, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(jobQueue, nameof(jobQueue));
        await _dbContext.Set<JobQueue>().AddAsync(jobQueue, cancellationToken).ConfigureAwait(false);
    }
}
