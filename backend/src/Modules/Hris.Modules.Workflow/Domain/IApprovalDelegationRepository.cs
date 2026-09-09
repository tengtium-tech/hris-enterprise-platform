namespace Hris.Modules.Workflow.Domain;

/// <summary>
/// Repository contract for the <see cref="ApprovalDelegation"/> Aggregate Root.
/// </summary>
public interface IApprovalDelegationRepository
{
    Task<ApprovalDelegation?> GetByIdAsync(ApprovalDelegationId id, CancellationToken cancellationToken);

    Task<IReadOnlyList<ApprovalDelegation>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken);

    /// <summary>
    /// Delegations naming <paramref name="delegatorUserAccountId"/> as delegator.
    /// Used to enforce WR-042: authority received by delegation cannot itself be
    /// delegated onward, which requires knowing what the prospective delegator
    /// already holds only by delegation.
    /// </summary>
    Task<IReadOnlyList<ApprovalDelegation>> ListByDelegatorAsync(
        Guid tenantId, Guid delegatorUserAccountId, CancellationToken cancellationToken);

    /// <summary>
    /// Delegations naming <paramref name="delegateUserAccountId"/> as delegate, which
    /// is what makes an onward-delegation attempt detectable.
    /// </summary>
    Task<IReadOnlyList<ApprovalDelegation>> ListByDelegateAsync(
        Guid tenantId, Guid delegateUserAccountId, CancellationToken cancellationToken);

    Task AddAsync(ApprovalDelegation delegation, CancellationToken cancellationToken);
}
