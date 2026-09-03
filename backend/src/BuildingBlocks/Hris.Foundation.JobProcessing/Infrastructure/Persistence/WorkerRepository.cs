using Hris.Foundation.JobProcessing.Domain;
using Hris.Infrastructure.Persistence;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hris.Foundation.JobProcessing.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IWorkerRepository"/>, per repositories.md's
/// "interface in the Domain layer... implementation in Infrastructure" split.
/// </summary>
internal sealed class WorkerRepository : IWorkerRepository
{
    private readonly HrisDbContext _dbContext;

    public WorkerRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public Task<Worker?> GetByIdAsync(WorkerId id, CancellationToken cancellationToken) =>
        _dbContext.Set<Worker>().FirstOrDefaultAsync(worker => worker.Id == id, cancellationToken);

    public async Task AddAsync(Worker worker, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(worker, nameof(worker));
        await _dbContext.Set<Worker>().AddAsync(worker, cancellationToken).ConfigureAwait(false);
    }
}
