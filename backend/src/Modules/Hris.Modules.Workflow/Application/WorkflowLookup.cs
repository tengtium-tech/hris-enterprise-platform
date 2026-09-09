using Hris.Modules.Workflow.Domain;
using Hris.SharedKernel;

namespace Hris.Modules.Workflow.Application;

/// <summary>
/// The one place every command and query handler that loads a
/// <see cref="WorkflowDefinition"/>, <see cref="ApprovalPolicy"/>, or
/// <see cref="ApprovalDelegation"/> by identifier performs this module's own
/// tenant-isolation check, per CTR-ISO-004. Returning not-found for both a
/// genuinely missing record and one that exists in a different tenant is deliberate
/// (CTR-ISO-002): a forbidden response would confirm the identifier exists
/// somewhere, which is the enumeration signal that requirement removes.
/// </summary>
internal static class WorkflowLookup
{
    public static async Task<Result<WorkflowDefinition>> LoadDefinitionForTenantAsync(
        IWorkflowDefinitionRepository repository, Guid definitionId, Guid tenantId, CancellationToken cancellationToken)
    {
        var definition = await repository.GetByIdAsync(new WorkflowDefinitionId(definitionId), cancellationToken)
            .ConfigureAwait(false);

        return definition is null || definition.TenantId != tenantId
            ? Result.Failure<WorkflowDefinition>(WorkflowErrors.DefinitionNotFound)
            : Result.Success(definition);
    }

    public static async Task<Result<ApprovalPolicy>> LoadPolicyForTenantAsync(
        IApprovalPolicyRepository repository, Guid tenantId, CancellationToken cancellationToken)
    {
        var policy = await repository.GetByTenantAsync(tenantId, cancellationToken).ConfigureAwait(false);

        return policy is null
            ? Result.Failure<ApprovalPolicy>(WorkflowErrors.ApprovalPolicyNotFound)
            : Result.Success(policy);
    }

    public static async Task<Result<ApprovalDelegation>> LoadDelegationForTenantAsync(
        IApprovalDelegationRepository repository, Guid delegationId, Guid tenantId, CancellationToken cancellationToken)
    {
        var delegation = await repository.GetByIdAsync(new ApprovalDelegationId(delegationId), cancellationToken)
            .ConfigureAwait(false);

        return delegation is null || delegation.TenantId != tenantId
            ? Result.Failure<ApprovalDelegation>(WorkflowErrors.DelegationNotFound)
            : Result.Success(delegation);
    }
}
