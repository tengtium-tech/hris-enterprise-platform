namespace Hris.Modules.Workflow.Domain;

/// <summary>
/// Repository contract for the <see cref="ApprovalPolicy"/> Aggregate Root.
/// <see cref="GetByTenantAsync"/> is the primary lookup rather than a by-identifier
/// one, because exactly one policy exists per tenant (WR-030) and every caller
/// reaches it that way.
/// </summary>
public interface IApprovalPolicyRepository
{
    Task<ApprovalPolicy?> GetByIdAsync(ApprovalPolicyId id, CancellationToken cancellationToken);

    Task<ApprovalPolicy?> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken);

    Task AddAsync(ApprovalPolicy policy, CancellationToken cancellationToken);
}
