using Hris.Infrastructure.Persistence;
using Hris.Modules.Leave.Domain;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hris.Modules.Leave.Infrastructure.Persistence;

/// <summary>EF Core implementation of <see cref="ILeaveRequestRepository"/>.</summary>
internal sealed class LeaveRequestRepository : ILeaveRequestRepository
{
    private readonly HrisDbContext _dbContext;

    public LeaveRequestRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public Task<LeaveRequest?> GetByIdAsync(LeaveRequestId id, CancellationToken cancellationToken) =>
        _dbContext.Set<LeaveRequest>().FirstOrDefaultAsync(request => request.Id == id, cancellationToken);

    public async Task<IReadOnlyList<LeaveRequest>> ListNonTerminalByEmployeeAsync(
        Guid tenantId, Guid employeeId, CancellationToken cancellationToken) =>
        await _dbContext.Set<LeaveRequest>()
            .Where(request => request.TenantId == tenantId && request.EmployeeId == employeeId
                               && request.Status != LeaveRequestStatus.Rejected && request.Status != LeaveRequestStatus.Cancelled)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<LeaveRequest>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken) =>
        await _dbContext.Set<LeaveRequest>()
            .Where(request => request.TenantId == tenantId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task AddAsync(LeaveRequest request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));
        await _dbContext.Set<LeaveRequest>().AddAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
