using Hris.Infrastructure.Persistence;
using Hris.Modules.Workflow.Domain;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hris.Modules.Workflow.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IApprovalDelegationRepository"/>.
/// </summary>
internal sealed class ApprovalDelegationRepository : IApprovalDelegationRepository
{
    private readonly HrisDbContext _dbContext;

    public ApprovalDelegationRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public Task<ApprovalDelegation?> GetByIdAsync(ApprovalDelegationId id, CancellationToken cancellationToken) =>
        _dbContext.Set<ApprovalDelegation>().FirstOrDefaultAsync(delegation => delegation.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ApprovalDelegation>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return await _dbContext.Set<ApprovalDelegation>()
            .Where(delegation => delegation.TenantId == tenantId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ApprovalDelegation>> ListByDelegatorAsync(
        Guid tenantId, Guid delegatorUserAccountId, CancellationToken cancellationToken)
    {
        return await _dbContext.Set<ApprovalDelegation>()
            .Where(delegation => delegation.TenantId == tenantId)
            .Where(delegation => delegation.DelegatorUserAccountId == delegatorUserAccountId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ApprovalDelegation>> ListByDelegateAsync(
        Guid tenantId, Guid delegateUserAccountId, CancellationToken cancellationToken)
    {
        return await _dbContext.Set<ApprovalDelegation>()
            .Where(delegation => delegation.TenantId == tenantId)
            .Where(delegation => delegation.DelegateUserAccountId == delegateUserAccountId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(ApprovalDelegation delegation, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(delegation, nameof(delegation));
        await _dbContext.Set<ApprovalDelegation>().AddAsync(delegation, cancellationToken).ConfigureAwait(false);
    }
}
