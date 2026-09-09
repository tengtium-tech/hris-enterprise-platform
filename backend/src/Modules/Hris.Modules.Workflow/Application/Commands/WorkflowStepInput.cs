using Hris.Modules.Workflow.Domain;
using Hris.SharedKernel;

namespace Hris.Modules.Workflow.Application.Commands;

/// <summary>
/// The authoring shape of one step, carried on
/// <see cref="EditDraftDefinitionCommand"/>. A flat, primitive-typed record rather
/// than the domain's own <see cref="WorkflowStep"/>: a command is a request object
/// crossing the application boundary, and accepting a constructed domain entity
/// there would let a caller bypass the entity's own construction invariants by
/// building one some other way.
///
/// <see cref="ToStep"/> is where the conversion happens, and it deliberately
/// surfaces the domain's own validation failures unchanged rather than pre-checking
/// them here, so the aggregate remains the single place a step's shape is decided.
/// </summary>
public sealed record WorkflowStepInput(
    Guid StepId,
    int Order,
    StepType StepType,
    string? Name,
    ApproverResolutionKind? ApproverResolutionKind,
    string? ApproverRoleName,
    Guid? ApproverScopeId,
    string? CommandModuleName,
    string? CommandId,
    string? ConditionExpression,
    IReadOnlyList<ConditionBranch>? ConditionBranches,
    IReadOnlyList<Guid>? Successors,
    Guid? DefaultSuccessor,
    JoinMode? JoinMode,
    bool NonDelegable,
    TimeSpan? EscalationTriggerAfter,
    ApproverResolutionKind? EscalationTargetKind,
    string? EscalationTargetRoleName,
    Guid? EscalationTargetScopeId,
    int? EscalationMaxDepth,
    TimeSpan? SlaOverride)
{
    public Result<WorkflowStep> ToStep()
    {
        ApproverResolutionRule? resolutionRule = null;
        if (ApproverResolutionKind is not null)
        {
            var ruleResult = BuildRule(ApproverResolutionKind.Value, ApproverRoleName, ApproverScopeId);
            if (ruleResult.IsFailure)
            {
                return Result.Failure<WorkflowStep>(ruleResult.Error);
            }

            resolutionRule = ruleResult.Value;
        }

        CommandReference? commandReference = null;
        if (CommandModuleName is not null || CommandId is not null)
        {
            var commandResult = CommandReference.Create(CommandModuleName, CommandId);
            if (commandResult.IsFailure)
            {
                return Result.Failure<WorkflowStep>(commandResult.Error);
            }

            commandReference = commandResult.Value;
        }

        StepCondition? condition = null;
        if (ConditionExpression is not null || ConditionBranches is not null)
        {
            var conditionResult = StepCondition.Create(ConditionExpression, ConditionBranches);
            if (conditionResult.IsFailure)
            {
                return Result.Failure<WorkflowStep>(conditionResult.Error);
            }

            condition = conditionResult.Value;
        }

        EscalationPolicy? escalation = null;
        if (EscalationTriggerAfter is not null)
        {
            if (EscalationTargetKind is null)
            {
                return Result.Failure<WorkflowStep>(WorkflowErrors.RoleResolutionRequiresRoleName);
            }

            var targetResult = BuildRule(EscalationTargetKind.Value, EscalationTargetRoleName, EscalationTargetScopeId);
            if (targetResult.IsFailure)
            {
                return Result.Failure<WorkflowStep>(targetResult.Error);
            }

            var escalationResult = EscalationPolicy.Create(
                EscalationTriggerAfter.Value, targetResult.Value, EscalationMaxDepth ?? 0);
            if (escalationResult.IsFailure)
            {
                return Result.Failure<WorkflowStep>(escalationResult.Error);
            }

            escalation = escalationResult.Value;
        }

        SlaDuration? sla = null;
        if (SlaOverride is not null)
        {
            var slaResult = SlaDuration.Create(SlaOverride.Value);
            if (slaResult.IsFailure)
            {
                return Result.Failure<WorkflowStep>(slaResult.Error);
            }

            sla = slaResult.Value;
        }

        return WorkflowStep.Create(
            new WorkflowStepId(StepId), Order, StepType, Name, resolutionRule, commandReference, condition, Successors,
            DefaultSuccessor, JoinMode, NonDelegable, escalation, sla);
    }

    private static Result<ApproverResolutionRule> BuildRule(
        ApproverResolutionKind kind, string? roleName, Guid? scopeId) => kind switch
        {
            Domain.ApproverResolutionKind.Role => ApproverResolutionRule.ForRole(roleName),
            Domain.ApproverResolutionKind.OrganizationalScope => ApproverResolutionRule.ForOrganizationalScope(roleName, scopeId),
            _ => scopeId is not null || roleName is not null
                ? Result.Failure<ApproverResolutionRule>(WorkflowErrors.ResolutionRuleTargetProhibited)
                : ApproverResolutionRule.ForReportingLine(),
        };
}
