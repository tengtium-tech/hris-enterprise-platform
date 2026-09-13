using Hris.Infrastructure.Persistence;
using Hris.Modules.Leave.Domain;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hris.Modules.Leave.Infrastructure.Persistence;

/// <summary>EF Core implementation of <see cref="ILeaveTypeRepository"/>.</summary>
internal sealed class LeaveTypeRepository : ILeaveTypeRepository
{
    private readonly HrisDbContext _dbContext;

    public LeaveTypeRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public Task<LeaveType?> GetByIdAsync(LeaveTypeId id, CancellationToken cancellationToken) =>
        _dbContext.Set<LeaveType>().FirstOrDefaultAsync(type => type.Id == id, cancellationToken);

    public async Task<IReadOnlyList<LeaveType>> ListCatalogForTenantAsync(Guid tenantId, CancellationToken cancellationToken) =>
        await _dbContext.Set<LeaveType>()
            .Where(type => type.TenantId == null || type.TenantId == tenantId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task AddAsync(LeaveType leaveType, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(leaveType, nameof(leaveType));
        await _dbContext.Set<LeaveType>().AddAsync(leaveType, cancellationToken).ConfigureAwait(false);
    }
}
