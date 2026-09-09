using Hris.Modules.Workflow.Domain;

namespace Hris.Modules.Workflow.Tests;

/// <summary>
/// Shared fixtures. Kept deliberately small: each builds the minimum valid shape a
/// test needs, so a test that fails does so because of the rule under test rather
/// than because of incidental fixture complexity.
/// </summary>
internal static class TestWorkflow
{
    public static readonly DateTimeOffset NowUtc = new(2026, 9, 9, 10, 0, 0, TimeSpan.Zero);

    public static DateOnly Today => DateOnly.FromDateTime(NowUtc.UtcDateTime);

    public static ApproverResolutionRule ReportingLineRule => ApproverResolutionRule.ForReportingLine().Value;

    public static ApproverResolutionRule RoleRule(string roleName = "HRManager") =>
        ApproverResolutionRule.ForRole(roleName).Value;

    public static SlaDuration Sla(int hours = 48) => SlaDuration.Create(TimeSpan.FromHours(hours)).Value;

    public static EscalationPolicy Escalation(int maxDepth = 2) =>
        EscalationPolicy.Create(TimeSpan.FromHours(24), RoleRule(), maxDepth).Value;

    public static WorkflowStep ApprovalStep(Guid id, int order, Guid? successor, bool nonDelegable = false) =>
        WorkflowStep.Create(
            new WorkflowStepId(id), order, StepType.Approval, $"Approval {order}", ReportingLineRule, null, null,
            successor is null ? [] : [successor.Value], null, null, nonDelegable, null, null).Value;

    public static WorkflowStep TerminalStep(Guid id, int order) =>
        WorkflowStep.Create(
            new WorkflowStepId(id), order, StepType.Terminal, $"End {order}", null, null, null, null, null, null, false,
            null, null).Value;

    public static WorkflowStep ConditionalStep(Guid id, int order, Guid branchTarget, Guid defaultTarget) =>
        WorkflowStep.Create(
            new WorkflowStepId(id), order, StepType.Conditional, $"Branch {order}", null, null,
            StepCondition.Create("amount", [new ConditionBranch("amount > 1000", branchTarget)]).Value,
            [branchTarget], defaultTarget, null, false, null, null).Value;

    /// <summary>A draft with one approval step leading to a terminal step: the smallest publishable graph.</summary>
    public static WorkflowDefinition PublishableDraft(Guid tenantId, out Guid approvalStepId)
    {
        var definition = Draft(tenantId);
        var terminalId = Guid.NewGuid();
        approvalStepId = Guid.NewGuid();

        definition.ReplaceSteps([ApprovalStep(approvalStepId, 1, terminalId), TerminalStep(terminalId, 2)]);
        return definition;
    }

    public static WorkflowDefinition Draft(Guid tenantId) =>
        WorkflowDefinition.Author(
            new WorkflowDefinitionId(Guid.NewGuid()), tenantId, Guid.NewGuid(), "Leave Approval", null, null, false,
            Guid.NewGuid(), NowUtc).Value;

    public static WorkflowDefinition Published(Guid tenantId)
    {
        var definition = PublishableDraft(tenantId, out _);
        definition.Publish(false, false, false, Guid.NewGuid(), NowUtc);
        return definition;
    }

    public static ApprovalPolicy Policy(Guid tenantId) =>
        ApprovalPolicy.Create(new ApprovalPolicyId(Guid.NewGuid()), tenantId, false, NowUtc).Value;

    public static ApprovalDelegation Delegation(
        Guid tenantId, Guid delegator, Guid delegateAccount, bool coversAll = true, IReadOnlyList<Guid>? scope = null) =>
        ApprovalDelegation.Create(
            new ApprovalDelegationId(Guid.NewGuid()), tenantId, delegator, delegateAccount, scope, coversAll,
            Today.AddDays(-1), Today.AddDays(10), "Annual leave cover", null, false, false, Guid.NewGuid(), NowUtc).Value;

    public static ApprovalDelegation ActiveDelegation(
        Guid tenantId, Guid delegator, Guid delegateAccount, bool coversAll = true, IReadOnlyList<Guid>? scope = null)
    {
        var delegation = Delegation(tenantId, delegator, delegateAccount, coversAll, scope);
        delegation.Activate(false, NowUtc);
        return delegation;
    }
}
