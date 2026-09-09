using Hris.Infrastructure.Persistence;
using Hris.Modules.Workflow.Domain;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hris.Modules.Workflow.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IApprovalPolicyRepository"/>.
/// </summary>
internal sealed class ApprovalPolicyRepository : IApprovalPolicyRepository
{
    private readonly HrisDbContext _dbContext;

    public ApprovalPolicyRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public Task<ApprovalPolicy?> GetByIdAsync(ApprovalPolicyId id, CancellationToken cancellationToken) =>
        _dbContext.Set<ApprovalPolicy>().FirstOrDefaultAsync(policy => policy.Id == id, cancellationToken);

    public Task<ApprovalPolicy?> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken) =>
        _dbContext.Set<ApprovalPolicy>().FirstOrDefaultAsync(policy => policy.TenantId == tenantId, cancellationToken);

    public async Task AddAsync(ApprovalPolicy policy, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(policy, nameof(policy));
        await _dbContext.Set<ApprovalPolicy>().AddAsync(policy, cancellationToken).ConfigureAwait(false);
    }
}
