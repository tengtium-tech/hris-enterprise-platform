using Hris.Infrastructure.Persistence;
using Hris.Modules.Leave.Domain;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hris.Modules.Leave.Infrastructure.Persistence;

/// <summary>EF Core implementation of <see cref="ILeaveEncashmentRepository"/>.</summary>
internal sealed class LeaveEncashmentRepository : ILeaveEncashmentRepository
{
    private readonly HrisDbContext _dbContext;

    public LeaveEncashmentRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public Task<LeaveEncashment?> GetByIdAsync(LeaveEncashmentId id, CancellationToken cancellationToken) =>
        _dbContext.Set<LeaveEncashment>().FirstOrDefaultAsync(encashment => encashment.Id == id, cancellationToken);

    public async Task<IReadOnlyList<LeaveEncashment>> ListPendingByBalanceAsync(
        Guid tenantId, LeaveBalanceId leaveBalanceId, CancellationToken cancellationToken) =>
        await _dbContext.Set<LeaveEncashment>()
            .Where(encashment => encashment.TenantId == tenantId && encashment.LeaveBalanceId == leaveBalanceId
                                  && encashment.Status == LeaveEncashmentStatus.PendingApproval)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<LeaveEncashment>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken) =>
        await _dbContext.Set<LeaveEncashment>()
            .Where(encashment => encashment.TenantId == tenantId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task AddAsync(LeaveEncashment encashment, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(encashment, nameof(encashment));
        await _dbContext.Set<LeaveEncashment>().AddAsync(encashment, cancellationToken).ConfigureAwait(false);
    }
}
