using Hris.Infrastructure.Persistence;
using Hris.Modules.Leave.Domain;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hris.Modules.Leave.Infrastructure.Persistence;

/// <summary>EF Core implementation of <see cref="ILeaveBalanceRepository"/>.</summary>
internal sealed class LeaveBalanceRepository : ILeaveBalanceRepository
{
    private readonly HrisDbContext _dbContext;

    public LeaveBalanceRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public Task<LeaveBalance?> GetByIdAsync(LeaveBalanceId id, CancellationToken cancellationToken) =>
        _dbContext.Set<LeaveBalance>().FirstOrDefaultAsync(balance => balance.Id == id, cancellationToken);

    public Task<LeaveBalance?> GetByEmployeeAndLeaveTypeAsync(
        Guid tenantId, Guid employeeId, LeaveTypeId leaveTypeId, CancellationToken cancellationToken) =>
        _dbContext.Set<LeaveBalance>().FirstOrDefaultAsync(
            balance => balance.TenantId == tenantId && balance.EmployeeId == employeeId && balance.LeaveTypeId == leaveTypeId,
            cancellationToken);

    public async Task<IReadOnlyList<LeaveBalance>> ListByEmployeeAsync(
        Guid tenantId, Guid employeeId, CancellationToken cancellationToken) =>
        await _dbContext.Set<LeaveBalance>()
            .Where(balance => balance.TenantId == tenantId && balance.EmployeeId == employeeId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task AddAsync(LeaveBalance balance, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(balance, nameof(balance));
        await _dbContext.Set<LeaveBalance>().AddAsync(balance, cancellationToken).ConfigureAwait(false);
    }
}
