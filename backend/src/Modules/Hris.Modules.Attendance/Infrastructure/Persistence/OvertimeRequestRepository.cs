using Hris.Infrastructure.Persistence;
using Hris.Modules.Attendance.Domain;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hris.Modules.Attendance.Infrastructure.Persistence;

/// <summary>EF Core implementation of <see cref="IOvertimeRequestRepository"/>.</summary>
internal sealed class OvertimeRequestRepository : IOvertimeRequestRepository
{
    private readonly HrisDbContext _dbContext;

    public OvertimeRequestRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public Task<OvertimeRequest?> GetByIdAsync(OvertimeRequestId id, CancellationToken cancellationToken) =>
        _dbContext.Set<OvertimeRequest>().FirstOrDefaultAsync(request => request.Id == id, cancellationToken);

    public async Task<IReadOnlyList<OvertimeRequest>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken) =>
        await _dbContext.Set<OvertimeRequest>()
            .Where(request => request.TenantId == tenantId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public Task<OvertimeRequest?> GetByEmployeeAndWorkDateAsync(
        Guid tenantId, Guid employeeId, DateOnly workDate, CancellationToken cancellationToken) =>
        _dbContext.Set<OvertimeRequest>()
            .FirstOrDefaultAsync(
                request => request.TenantId == tenantId
                    && request.EmployeeId == employeeId
                    && request.WorkDate == workDate,
                cancellationToken);

    public async Task<IReadOnlyList<OvertimeRequest>> ListApprovedForWorkDateAsync(
        Guid tenantId, DateOnly workDate, CancellationToken cancellationToken) =>
        await _dbContext.Set<OvertimeRequest>()
            .Where(request => request.TenantId == tenantId
                && request.WorkDate == workDate
                && request.Status == ApprovalStatus.Approved)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task AddAsync(OvertimeRequest request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));
        await _dbContext.Set<OvertimeRequest>().AddAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
