using Hris.Infrastructure.Persistence;
using Hris.Modules.Leave.Domain;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hris.Modules.Leave.Infrastructure.Persistence;

/// <summary>EF Core implementation of <see cref="ILeavePolicyRepository"/>.</summary>
internal sealed class LeavePolicyRepository : ILeavePolicyRepository
{
    private readonly HrisDbContext _dbContext;

    public LeavePolicyRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public Task<LeavePolicy?> GetByIdAsync(LeavePolicyId id, CancellationToken cancellationToken) =>
        _dbContext.Set<LeavePolicy>().FirstOrDefaultAsync(policy => policy.Id == id, cancellationToken);

    public async Task<IReadOnlyList<LeavePolicy>> ListByLineageAsync(Guid lineageId, CancellationToken cancellationToken) =>
        await _dbContext.Set<LeavePolicy>()
            .Where(policy => policy.LineageId == lineageId)
            .OrderBy(policy => policy.Version)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<LeavePolicy>> ListEffectiveForLeaveTypeAsync(
        Guid tenantId, Guid leaveTypeId, DateOnly asOfDate, CancellationToken cancellationToken)
    {
        var typedLeaveTypeId = new LeaveTypeId(leaveTypeId);

        return await _dbContext.Set<LeavePolicy>()
            .Where(policy => policy.TenantId == tenantId
                && policy.LeaveTypeId == typedLeaveTypeId
                && policy.Status == LeavePolicyStatus.Active
                && policy.EffectiveFrom != null && policy.EffectiveFrom <= asOfDate
                && (policy.EffectiveTo == null || asOfDate <= policy.EffectiveTo.Value))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(LeavePolicy policy, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(policy, nameof(policy));
        await _dbContext.Set<LeavePolicy>().AddAsync(policy, cancellationToken).ConfigureAwait(false);
    }
}
