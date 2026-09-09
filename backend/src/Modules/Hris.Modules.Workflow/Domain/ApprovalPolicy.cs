using Hris.SharedKernel;

namespace Hris.Modules.Workflow.Domain;

/// <summary>
/// Aggregate Root holding a tenant's approval governance. Source:
/// docs/04-modules/workflow/domain/aggregates.md.
///
/// Exactly one per tenant (WR-030), so there is no "which policy applies" question
/// to resolve at evaluation time.
///
/// It is separate from <see cref="WorkflowDefinition"/> because it governs every
/// definition and changes for reasons none of them cause. Embedding the default
/// escalation period in each definition would mean a tenant-wide governance change
/// required editing and republishing every definition, and because publication
/// creates a new version (WR-004), that would version every process for a change
/// belonging to none of them. The separation also keeps the security boundary
/// clean: authoring a definition and changing the tenant's approval governance are
/// different permissions, and the second is materially more powerful.
///
/// This type deliberately holds no self-approval setting (WR-032). That is not an
/// omission to be filled in later; it is the point. The top-of-hierarchy exemption
/// is evaluated structurally by <see cref="ApproverResolution"/> at resolution time,
/// never stored as tenant policy, because a configurable exception is exactly what
/// CTR-WFL-002 exists to prevent.
/// </summary>
public sealed class ApprovalPolicy : AggregateRoot<ApprovalPolicyId>
{
    public Guid TenantId { get; }

    /// <summary>
    /// The business processes this tenant gates with approval. A process absent from
    /// this set has no definition and creates no instance: workflow-definitions.md
    /// is explicit that modelling "no approval required" as a trivial always-approve
    /// definition would be wrong, since it would create instances, history records,
    /// and engine load for a gate the tenant deliberately chose not to apply.
    /// </summary>
    public IReadOnlyList<Guid> ProcessesRequiringApproval { get; private set; } = [];

    public EscalationPolicy? DefaultEscalation { get; internal set; }

    public SlaDuration? DefaultSla { get; internal set; }

    public IReadOnlyList<ApprovalAuthorityLimit> AuthorityLimits { get; private set; } = [];

    /// <summary>
    /// Whether the tenant may author custom definitions at all. Entitlement-bounded
    /// (the Automation pack, ADM-004). Where a pack is downgraded, WR-052 withdraws
    /// only the ability to author or publish; existing definitions stay readable and
    /// running instances complete, because terminating in-flight approvals would
    /// strand business requests that are not the request's fault.
    /// </summary>
    public bool CustomDefinitionsPermitted { get; private set; }

    public Guid? LastConfiguredBy { get; private set; }

    public DateTimeOffset? LastConfiguredOn { get; private set; }

    public string? LastConfigurationReason { get; private set; }

    public DateTimeOffset CreatedOn { get; }

    private ApprovalPolicy(ApprovalPolicyId id, Guid tenantId, DateTimeOffset createdOn)
        : base(id)
    {
        TenantId = tenantId;
        CreatedOn = createdOn;
    }

    /// <summary>
    /// <paramref name="policyAlreadyExistsForTenant"/> is computed by the calling
    /// handler and enforces WR-030's singleton, a fact this aggregate cannot see
    /// about itself.
    /// </summary>
    public static Result<ApprovalPolicy> Create(
        ApprovalPolicyId id, Guid tenantId, bool policyAlreadyExistsForTenant, DateTimeOffset nowUtc)
    {
        return policyAlreadyExistsForTenant
            ? Result.Failure<ApprovalPolicy>(WorkflowErrors.ApprovalPolicyAlreadyExistsForTenant)
            : Result.Success(new ApprovalPolicy(id, tenantId, nowUtc));
    }

    /// <summary>
    /// One command covers the whole policy surface, per commands.md: each value
    /// changes what is permitted tenant-wide, each requires the same permission, and
    /// each belongs in access-review reporting rather than a routine configuration
    /// log.
    ///
    /// A reason is required. A policy change that stops requiring approval for a
    /// business process is a control removal, and a control removal without a stated
    /// reason is the change most worth having one for.
    ///
    /// <paramref name="anyLimitExceedsRoleStanding"/> is WR-031, computed by the
    /// caller against Authorization Framework data this module does not own.
    /// </summary>
    public Result Configure(
        IReadOnlyList<Guid>? processesRequiringApproval, EscalationPolicy? defaultEscalation, SlaDuration? defaultSla,
        IReadOnlyList<ApprovalAuthorityLimit>? authorityLimits, bool customDefinitionsPermitted,
        bool anyLimitExceedsRoleStanding, Guid configuredBy, string? reason, DateTimeOffset nowUtc)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result.Failure(WorkflowErrors.PolicyChangeReasonRequired);
        }

        if (anyLimitExceedsRoleStanding)
        {
            return Result.Failure(WorkflowErrors.AuthorityLimitExceedsRoleStanding);
        }

        ProcessesRequiringApproval = processesRequiringApproval?.Distinct().ToList() ?? [];
        DefaultEscalation = defaultEscalation;
        DefaultSla = defaultSla;
        AuthorityLimits = authorityLimits?.ToList() ?? [];
        CustomDefinitionsPermitted = customDefinitionsPermitted;
        LastConfiguredBy = configuredBy;
        LastConfiguredOn = nowUtc;
        LastConfigurationReason = reason.Trim();

        AddDomainEvent(new ApprovalPolicyConfigured(Guid.NewGuid(), nowUtc, Id, TenantId, configuredBy, reason.Trim()));

        return Result.Success();
    }

    public bool RequiresApproval(Guid businessProcessId) => ProcessesRequiringApproval.Contains(businessProcessId);
}
