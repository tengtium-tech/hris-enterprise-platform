using FluentAssertions;
using Hris.Modules.Workflow.Application.Commands;
using Hris.Modules.Workflow.Domain;
using Xunit;

namespace Hris.Modules.Workflow.Tests.Application;

/// <summary>
/// The command-boundary conversion. It surfaces the domain's own validation failures
/// unchanged rather than pre-checking them, so the aggregate stays the single place
/// a step's shape is decided.
/// </summary>
public sealed class WorkflowStepInputTests
{
    private static WorkflowStepInput Input(
        StepType stepType = StepType.Terminal,
        ApproverResolutionKind? approverKind = null,
        string? approverRole = null,
        Guid? approverScope = null,
        string? commandModule = null,
        string? commandId = null,
        string? conditionExpression = null,
        IReadOnlyList<ConditionBranch>? branches = null,
        IReadOnlyList<Guid>? successors = null,
        Guid? defaultSuccessor = null,
        TimeSpan? escalationTrigger = null,
        ApproverResolutionKind? escalationKind = null,
        string? escalationRole = null,
        int? escalationDepth = null,
        TimeSpan? sla = null) => new(
        Guid.NewGuid(), 1, stepType, "Step", approverKind, approverRole, approverScope, commandModule, commandId,
        conditionExpression, branches, successors, defaultSuccessor, null, false, escalationTrigger, escalationKind,
        escalationRole, null, escalationDepth, sla);

    [Fact]
    public void ToStep_BuildsATerminalStep()
    {
        Input().ToStep().IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ToStep_BuildsARoleResolvedApprovalStep()
    {
        var result = Input(
            StepType.Approval, ApproverResolutionKind.Role, "HRManager", successors: [Guid.NewGuid()]).ToStep();

        result.IsSuccess.Should().BeTrue();
        result.Value.ApproverResolutionRule!.RoleName.Should().Be("HRManager");
    }

    [Fact]
    public void ToStep_BuildsAReportingLineApprovalStep()
    {
        var result = Input(
            StepType.Approval, ApproverResolutionKind.ReportingLine, successors: [Guid.NewGuid()]).ToStep();

        result.IsSuccess.Should().BeTrue();
        result.Value.ApproverResolutionRule!.Kind.Should().Be(ApproverResolutionKind.ReportingLine);
    }

    [Fact]
    public void ToStep_BuildsAScopeResolvedApprovalStep()
    {
        var scope = Guid.NewGuid();

        var result = Input(
            StepType.Approval, ApproverResolutionKind.OrganizationalScope, "HRManager", scope,
            successors: [Guid.NewGuid()]).ToStep();

        result.IsSuccess.Should().BeTrue();
        result.Value.ApproverResolutionRule!.ScopeId.Should().Be(scope);
    }

    [Fact]
    public void ToStep_Fails_WhenAReportingLineRuleCarriesATarget()
    {
        var result = Input(
            StepType.Approval, ApproverResolutionKind.ReportingLine, "HRManager", successors: [Guid.NewGuid()]).ToStep();

        result.Error.Should().Be(WorkflowErrors.ResolutionRuleTargetProhibited);
    }

    [Fact]
    public void ToStep_Fails_WhenARoleRuleHasNoRoleName()
    {
        var result = Input(StepType.Approval, ApproverResolutionKind.Role, successors: [Guid.NewGuid()]).ToStep();

        result.Error.Should().Be(WorkflowErrors.RoleResolutionRequiresRoleName);
    }

    [Fact]
    public void ToStep_BuildsAnAutomatedStepsCommandReference()
    {
        var result = Input(
            StepType.Automated, commandModule: "Leave", commandId: "ApproveLeave", successors: [Guid.NewGuid()]).ToStep();

        result.IsSuccess.Should().BeTrue();
        result.Value.CommandReference!.ToString().Should().Be("Leave.ApproveLeave");
    }

    [Fact]
    public void ToStep_Fails_WhenACommandReferenceIsIncomplete()
    {
        var result = Input(StepType.Automated, commandModule: "Leave", successors: [Guid.NewGuid()]).ToStep();

        result.Error.Should().Be(WorkflowErrors.CommandReferenceCommandRequired);
    }

    [Fact]
    public void ToStep_BuildsAConditionalStep()
    {
        var target = Guid.NewGuid();

        var result = Input(
            StepType.Conditional, conditionExpression: "amount", branches: [new ConditionBranch("amount > 5", target)],
            successors: [target], defaultSuccessor: Guid.NewGuid()).ToStep();

        result.IsSuccess.Should().BeTrue();
        result.Value.Condition!.Branches.Should().ContainSingle();
    }

    [Fact]
    public void ToStep_Fails_WhenAConditionHasNoBranches()
    {
        var result = Input(
            StepType.Conditional, conditionExpression: "amount", successors: [Guid.NewGuid()],
            defaultSuccessor: Guid.NewGuid()).ToStep();

        result.Error.Should().Be(WorkflowErrors.StepConditionRequiresBranches);
    }

    [Fact]
    public void ToStep_BuildsAnEscalationOverride()
    {
        var result = Input(
            StepType.Approval, ApproverResolutionKind.ReportingLine, successors: [Guid.NewGuid()],
            escalationTrigger: TimeSpan.FromHours(24), escalationKind: ApproverResolutionKind.Role,
            escalationRole: "HRManager", escalationDepth: 2).ToStep();

        result.IsSuccess.Should().BeTrue();
        result.Value.EscalationOverride!.MaxDepth.Should().Be(2);
    }

    [Fact]
    public void ToStep_Fails_WhenAnEscalationTriggerHasNoTargetKind()
    {
        var result = Input(
            StepType.Approval, ApproverResolutionKind.ReportingLine, successors: [Guid.NewGuid()],
            escalationTrigger: TimeSpan.FromHours(24)).ToStep();

        result.Error.Should().Be(WorkflowErrors.RoleResolutionRequiresRoleName);
    }

    [Fact]
    public void ToStep_Fails_WhenTheEscalationTargetIsInvalid()
    {
        var result = Input(
            StepType.Approval, ApproverResolutionKind.ReportingLine, successors: [Guid.NewGuid()],
            escalationTrigger: TimeSpan.FromHours(24), escalationKind: ApproverResolutionKind.Role,
            escalationDepth: 2).ToStep();

        result.Error.Should().Be(WorkflowErrors.RoleResolutionRequiresRoleName);
    }

    [Fact]
    public void ToStep_Fails_WhenTheEscalationDepthIsUnbounded()
    {
        var result = Input(
            StepType.Approval, ApproverResolutionKind.ReportingLine, successors: [Guid.NewGuid()],
            escalationTrigger: TimeSpan.FromHours(24), escalationKind: ApproverResolutionKind.Role,
            escalationRole: "HRManager", escalationDepth: EscalationPolicy.MaximumDepth + 1).ToStep();

        result.Error.Should().Be(WorkflowErrors.EscalationDepthUnbounded);
    }

    [Fact]
    public void ToStep_BuildsAnSlaOverride()
    {
        var result = Input(sla: TimeSpan.FromHours(12)).ToStep();

        result.IsSuccess.Should().BeTrue();
        result.Value.SlaOverride!.Value.Should().Be(TimeSpan.FromHours(12));
    }

    [Fact]
    public void ToStep_Fails_WhenTheSlaOverrideIsNotPositive()
    {
        var result = Input(sla: TimeSpan.Zero).ToStep();

        result.Error.Should().Be(WorkflowErrors.SlaDurationMustBePositive);
    }
}
