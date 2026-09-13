using Hris.Infrastructure.Persistence;
using Hris.Modules.Leave.Domain;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hris.Modules.Leave.Infrastructure.Persistence;

/// <summary>EF Core implementation of <see cref="ILeaveAdjustmentRepository"/>.</summary>
internal sealed class LeaveAdjustmentRepository : ILeaveAdjustmentRepository
{
    private readonly HrisDbContext _dbContext;

    public LeaveAdjustmentRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public Task<LeaveAdjustment?> GetByIdAsync(LeaveAdjustmentId id, CancellationToken cancellationToken) =>
        _dbContext.Set<LeaveAdjustment>().FirstOrDefaultAsync(adjustment => adjustment.Id == id, cancellationToken);

    public async Task<IReadOnlyList<LeaveAdjustment>> ListPendingByBalanceAsync(
        Guid tenantId, LeaveBalanceId leaveBalanceId, CancellationToken cancellationToken) =>
        await _dbContext.Set<LeaveAdjustment>()
            .Where(adjustment => adjustment.TenantId == tenantId && adjustment.LeaveBalanceId == leaveBalanceId
                                  && adjustment.Status != LeaveAdjustmentStatus.Rejected
                                  && adjustment.Status != LeaveAdjustmentStatus.Cancelled
                                  && adjustment.Status != LeaveAdjustmentStatus.Applied)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<LeaveAdjustment>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken) =>
        await _dbContext.Set<LeaveAdjustment>()
            .Where(adjustment => adjustment.TenantId == tenantId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task AddAsync(LeaveAdjustment adjustment, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(adjustment, nameof(adjustment));
        await _dbContext.Set<LeaveAdjustment>().AddAsync(adjustment, cancellationToken).ConfigureAwait(false);
    }
}
