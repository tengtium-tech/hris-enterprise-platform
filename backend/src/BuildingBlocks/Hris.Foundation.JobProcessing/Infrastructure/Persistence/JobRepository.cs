using Hris.Foundation.JobProcessing.Domain;
using Hris.Infrastructure.Persistence;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hris.Foundation.JobProcessing.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IJobRepository"/>, per repositories.md's
/// "interface in the Domain layer... implementation in Infrastructure" split.
/// </summary>
internal sealed class JobRepository : IJobRepository
{
    private readonly HrisDbContext _dbContext;

    public JobRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public Task<Job?> GetByIdAsync(JobId id, CancellationToken cancellationToken) =>
        _dbContext.Set<Job>().FirstOrDefaultAsync(job => job.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Job>> ListByQueueAsync(
        JobQueueId jobQueueId, Guid tenantId, int maxResults, CancellationToken cancellationToken) =>
        await _dbContext.Set<Job>()
            .Where(job => job.JobQueueId == jobQueueId && job.TenantId == tenantId)
            .OrderByDescending(job => job.SubmittedAtUtc)
            .Take(maxResults)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task AddAsync(Job job, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(job, nameof(job));
        await _dbContext.Set<Job>().AddAsync(job, cancellationToken).ConfigureAwait(false);
    }
}
