using Hris.Modules.Workflow.Application.Dtos;
using Hris.Modules.Workflow.Domain;

namespace Hris.Modules.Workflow.Application.Mapping;

/// <summary>
/// Explicit hand-written projection from aggregate to DTO, per mapping.md's own
/// convention across every prior module: no reflection-based mapper, so what leaves
/// the module is visible in source and a newly added domain property never escapes
/// into a response by default.
/// </summary>
internal static class WorkflowMapper
{
    public static WorkflowDefinitionDto ToDto(WorkflowDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        return new WorkflowDefinitionDto(
            definition.Id.Value,
            definition.TenantId,
            definition.BusinessProcessId,
            definition.LineageId,
            definition.Name,
            definition.Description,
            definition.TriggerCondition,
            definition.Version,
            definition.Status.ToString(),
            definition.Steps.Select(ToDto).ToList(),
            definition.CreatedBy,
            definition.CreatedOn,
            definition.PublishedBy,
            definition.PublishedOn,
            definition.DeprecatedBy,
            definition.DeprecatedOn,
            definition.DeprecationReason);
    }

    public static WorkflowStepDto ToDto(WorkflowStep step)
    {
        ArgumentNullException.ThrowIfNull(step);

        return new WorkflowStepDto(
            step.Id.Value,
            step.Order,
            step.StepType.ToString(),
            step.Name,
            step.ApproverResolutionRule is null ? null : ToDto(step.ApproverResolutionRule),
            step.CommandReference is null ? null : new CommandReferenceDto(step.CommandReference.ModuleName, step.CommandReference.CommandId),
            step.Condition is null ? null : ToDto(step.Condition),
            step.Successors,
            step.DefaultSuccessor,
            step.JoinMode?.ToString(),
            step.NonDelegable,
            step.EscalationOverride is null ? null : ToDto(step.EscalationOverride),
            step.SlaOverride?.Value);
    }

    public static ApproverResolutionRuleDto ToDto(ApproverResolutionRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        return new ApproverResolutionRuleDto(rule.Kind.ToString(), rule.RoleName, rule.ScopeId);
    }

    public static StepConditionDto ToDto(StepCondition condition)
    {
        ArgumentNullException.ThrowIfNull(condition);

        return new StepConditionDto(
            condition.Expression,
            condition.Branches.Select(branch => new ConditionBranchDto(branch.Predicate, branch.TargetStepId)).ToList());
    }

    public static EscalationPolicyDto ToDto(EscalationPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        return new EscalationPolicyDto(policy.TriggerAfter, ToDto(policy.EscalateTo), policy.MaxDepth);
    }

    public static ApprovalPolicyDto ToDto(ApprovalPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        return new ApprovalPolicyDto(
            policy.Id.Value,
            policy.TenantId,
            policy.ProcessesRequiringApproval,
            policy.DefaultEscalation is null ? null : ToDto(policy.DefaultEscalation),
            policy.DefaultSla?.Value,
            policy.AuthorityLimits
                .Select(limit => new ApprovalAuthorityLimitDto(limit.RoleName, limit.ScopeId, limit.MaximumAmount))
                .ToList(),
            policy.CustomDefinitionsPermitted,
            policy.LastConfiguredBy,
            policy.LastConfiguredOn,
            policy.LastConfigurationReason,
            policy.CreatedOn);
    }

    public static ApprovalDelegationDto ToDto(ApprovalDelegation delegation)
    {
        ArgumentNullException.ThrowIfNull(delegation);

        return new ApprovalDelegationDto(
            delegation.Id.Value,
            delegation.TenantId,
            delegation.DelegatorUserAccountId,
            delegation.DelegateUserAccountId,
            delegation.Scope,
            delegation.CoversAllProcesses,
            delegation.Period.Start,
            delegation.Period.End,
            delegation.Reason,
            delegation.ApprovalReference,
            delegation.Status.ToString(),
            delegation.CreatedBy,
            delegation.CreatedOn,
            delegation.RevokedBy,
            delegation.RevokedOn);
    }
}
