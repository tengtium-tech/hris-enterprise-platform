using FluentAssertions;
using Hris.Modules.Workflow.Domain;
using Xunit;

namespace Hris.Modules.Workflow.Tests.Domain;

/// <summary>
/// entities.md's presence rules, enforced as construction invariants rather than
/// nullable fields validated later.
/// </summary>
public sealed class WorkflowStepTests
{
    private static WorkflowStepId NewId() => new(Guid.NewGuid());

    [Fact]
    public void Create_Fails_WhenNameIsMissing()
    {
        var result = WorkflowStep.Create(
            NewId(), 1, StepType.Terminal, " ", null, null, null, null, null, null, false, null, null);

        result.Error.Should().Be(WorkflowErrors.StepNameRequired);
    }

    [Fact]
    public void Create_Fails_WhenAnApprovalStepHasNoResolutionRule()
    {
        var result = WorkflowStep.Create(
            NewId(), 1, StepType.Approval, "Approve", null, null, null, [Guid.NewGuid()], null, null, false, null, null);

        result.Error.Should().Be(WorkflowErrors.ApprovalStepRequiresResolutionRule);
    }

    [Fact]
    public void Create_Fails_WhenANonApprovalStepCarriesAResolutionRule()
    {
        var result = WorkflowStep.Create(
            NewId(), 1, StepType.Terminal, "End", TestWorkflow.ReportingLineRule, null, null, null, null, null, false,
            null, null);

        result.Error.Should().Be(WorkflowErrors.ApprovalStepRequiresResolutionRule);
    }

    [Fact]
    public void Create_Fails_WhenAnAutomatedStepHasNoCommandReference()
    {
        var result = WorkflowStep.Create(
            NewId(), 1, StepType.Automated, "Run", null, null, null, [Guid.NewGuid()], null, null, false, null, null);

        result.Error.Should().Be(WorkflowErrors.AutomatedStepRequiresCommandReference);
    }

    [Fact]
    public void Create_Fails_WhenANonAutomatedStepCarriesACommandReference()
    {
        var command = CommandReference.Create("Leave", "ApproveLeave").Value;

        var result = WorkflowStep.Create(
            NewId(), 1, StepType.Terminal, "End", null, command, null, null, null, null, false, null, null);

        result.Error.Should().Be(WorkflowErrors.AutomatedStepRequiresCommandReference);
    }

    [Fact]
    public void Create_Fails_WhenAConditionalStepHasNoCondition()
    {
        var result = WorkflowStep.Create(
            NewId(), 1, StepType.Conditional, "Branch", null, null, null, [Guid.NewGuid()], Guid.NewGuid(), null, false,
            null, null);

        result.Error.Should().Be(WorkflowErrors.ConditionalStepRequiresCondition);
    }

    /// <summary>
    /// WR-003's construction half. The document calls this the rule whose absence
    /// fails silently: an instance reaching a condition matching no branch does not
    /// error, it simply stops, holding a business request indefinitely.
    /// </summary>
    [Fact]
    public void Create_Fails_WhenAConditionalStepHasNoDefaultSuccessor()
    {
        var condition = StepCondition.Create("amount", [new ConditionBranch("amount > 10", Guid.NewGuid())]).Value;

        var result = WorkflowStep.Create(
            NewId(), 1, StepType.Conditional, "Branch", null, null, condition, [Guid.NewGuid()], null, null, false, null,
            null);

        result.Error.Should().Be(WorkflowErrors.ConditionalStepRequiresDefaultSuccessor);
    }

    [Fact]
    public void Create_Fails_WhenATerminalStepHasSuccessors()
    {
        var result = WorkflowStep.Create(
            NewId(), 1, StepType.Terminal, "End", null, null, null, [Guid.NewGuid()], null, null, false, null, null);

        result.Error.Should().Be(WorkflowErrors.TerminalStepCannotHaveSuccessors);
    }

    [Fact]
    public void Create_Fails_WhenATerminalStepCarriesAJoinMode()
    {
        var result = WorkflowStep.Create(
            NewId(), 1, StepType.Terminal, "End", null, null, null, null, null, JoinMode.All, false, null, null);

        result.Error.Should().Be(WorkflowErrors.JoinModeRequiresMultiplePredecessors);
    }

    [Fact]
    public void Create_Succeeds_ForAnApprovalStepWithOverrides()
    {
        var result = WorkflowStep.Create(
            NewId(), 1, StepType.Approval, "Approve", TestWorkflow.ReportingLineRule, null, null, [Guid.NewGuid()], null,
            null, true, TestWorkflow.Escalation(), TestWorkflow.Sla());

        result.IsSuccess.Should().BeTrue();
        result.Value.NonDelegable.Should().BeTrue();
        result.Value.EscalationOverride.Should().NotBeNull();
        result.Value.SlaOverride.Should().NotBeNull();
    }

    [Fact]
    public void AllTargets_IncludesSuccessorsAndTheDefaultPath()
    {
        var branch = Guid.NewGuid();
        var fallback = Guid.NewGuid();
        var step = TestWorkflow.ConditionalStep(Guid.NewGuid(), 1, branch, fallback);

        step.AllTargets().Should().BeEquivalentTo([branch, fallback]);
    }

    [Fact]
    public void AllTargets_IsEmpty_ForATerminalStep()
    {
        var step = TestWorkflow.TerminalStep(Guid.NewGuid(), 1);

        step.AllTargets().Should().BeEmpty();
    }

    [Fact]
    public void EqualityIsByIdentity_NotByValue()
    {
        var id = NewId();
        var first = WorkflowStep.Create(
            id, 1, StepType.Terminal, "End A", null, null, null, null, null, null, false, null, null).Value;
        var second = WorkflowStep.Create(
            id, 9, StepType.Terminal, "End B", null, null, null, null, null, null, false, null, null).Value;

        first.Should().Be(second);
    }
}
