using Hris.Foundation.Scheduling.Domain;
using Hris.Infrastructure.Persistence;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hris.Foundation.Scheduling.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IScheduleRepository"/>, per repositories.md's
/// "interface in the Domain layer... implementation in Infrastructure" split. No
/// <c>UpdateAsync</c>: an aggregate loaded through <see cref="GetByIdAsync"/> is
/// already tracked by this same <see cref="HrisDbContext"/>, so the caller's own
/// <c>TransactionBehavior</c> persists any mutation via change tracking alone.
/// </summary>
internal sealed class ScheduleRepository : IScheduleRepository
{
    private readonly HrisDbContext _dbContext;

    public ScheduleRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public Task<Schedule?> GetByIdAsync(ScheduleId id, CancellationToken cancellationToken) =>
        _dbContext.Set<Schedule>().FirstOrDefaultAsync(schedule => schedule.Id == id, cancellationToken);

    public async Task AddAsync(Schedule schedule, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(schedule, nameof(schedule));
        await _dbContext.Set<Schedule>().AddAsync(schedule, cancellationToken).ConfigureAwait(false);
    }
}
