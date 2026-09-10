using Hris.Infrastructure.Persistence;
using Hris.Modules.Timekeeping.Domain;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hris.Modules.Timekeeping.Infrastructure.Persistence;

/// <summary>EF Core implementation of <see cref="IWorkScheduleRepository"/>.</summary>
internal sealed class WorkScheduleRepository : IWorkScheduleRepository
{
    private readonly HrisDbContext _dbContext;

    public WorkScheduleRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public Task<WorkSchedule?> GetByIdAsync(WorkScheduleId id, CancellationToken cancellationToken) =>
        _dbContext.Set<WorkSchedule>().FirstOrDefaultAsync(schedule => schedule.Id == id, cancellationToken);

    public async Task<IReadOnlyList<WorkSchedule>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken) =>
        await _dbContext.Set<WorkSchedule>()
            .Where(schedule => schedule.TenantId == tenantId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<WorkSchedule>> ListByLineageAsync(Guid lineageId, CancellationToken cancellationToken) =>
        await _dbContext.Set<WorkSchedule>()
            .Where(schedule => schedule.LineageId == lineageId)
            .OrderBy(schedule => schedule.Version)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task AddAsync(WorkSchedule schedule, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(schedule, nameof(schedule));
        await _dbContext.Set<WorkSchedule>().AddAsync(schedule, cancellationToken).ConfigureAwait(false);
    }
}
