using Hris.Infrastructure.Persistence;
using Hris.Modules.Timekeeping.Domain;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hris.Modules.Timekeeping.Infrastructure.Persistence;

/// <summary>EF Core implementation of <see cref="IWorkShiftRepository"/>.</summary>
internal sealed class WorkShiftRepository : IWorkShiftRepository
{
    private readonly HrisDbContext _dbContext;

    public WorkShiftRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public Task<WorkShift?> GetByIdAsync(WorkShiftId id, CancellationToken cancellationToken) =>
        _dbContext.Set<WorkShift>().FirstOrDefaultAsync(shift => shift.Id == id, cancellationToken);

    public async Task<IReadOnlyList<WorkShift>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken) =>
        await _dbContext.Set<WorkShift>()
            .Where(shift => shift.TenantId == tenantId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<WorkShift>> ListByLineageAsync(Guid lineageId, CancellationToken cancellationToken) =>
        await _dbContext.Set<WorkShift>()
            .Where(shift => shift.LineageId == lineageId)
            .OrderBy(shift => shift.Version)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<bool> CodeExistsInTenantAsync(
        Guid tenantId, string code, WorkShiftId? excluding, CancellationToken cancellationToken) =>
        await _dbContext.Set<WorkShift>()
            .Where(shift => shift.TenantId == tenantId)
            .Where(shift => shift.Code.Value == code)
            .Where(shift => excluding == null || shift.Id != excluding)
            .AnyAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task AddAsync(WorkShift shift, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(shift, nameof(shift));
        await _dbContext.Set<WorkShift>().AddAsync(shift, cancellationToken).ConfigureAwait(false);
    }
}
