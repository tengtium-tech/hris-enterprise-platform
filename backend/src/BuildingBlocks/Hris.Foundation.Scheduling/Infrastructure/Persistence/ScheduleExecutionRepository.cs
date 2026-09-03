using Hris.Foundation.Scheduling.Domain;
using Hris.Infrastructure.Persistence;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hris.Foundation.Scheduling.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IScheduleExecutionRepository"/>, per
/// repositories.md's "interface in the Domain layer... implementation in
/// Infrastructure" split.
/// </summary>
internal sealed class ScheduleExecutionRepository : IScheduleExecutionRepository
{
    private readonly HrisDbContext _dbContext;

    public ScheduleExecutionRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public Task<ScheduleExecution?> GetByIdAsync(ScheduleExecutionId id, CancellationToken cancellationToken) =>
        _dbContext.Set<ScheduleExecution>().FirstOrDefaultAsync(execution => execution.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ScheduleExecution>> ListByScheduleAsync(
        ScheduleId scheduleId, Guid tenantId, int maxResults, CancellationToken cancellationToken) =>
        await _dbContext.Set<ScheduleExecution>()
            .Where(execution => execution.ScheduleId == scheduleId && execution.TenantId == tenantId)
            .OrderByDescending(execution => execution.TriggeredAtUtc)
            .Take(maxResults)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task AddAsync(ScheduleExecution execution, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(execution, nameof(execution));
        await _dbContext.Set<ScheduleExecution>().AddAsync(execution, cancellationToken).ConfigureAwait(false);
    }
}
