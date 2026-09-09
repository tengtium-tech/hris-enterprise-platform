using FluentAssertions;
using Hris.Modules.Workflow.Domain;
using Xunit;

namespace Hris.Modules.Workflow.Tests.Domain;

public sealed class ApproverResolutionRuleTests
{
    [Fact]
    public void ForRole_Fails_WhenRoleNameIsMissing()
    {
        ApproverResolutionRule.ForRole("  ").Error.Should().Be(WorkflowErrors.RoleResolutionRequiresRoleName);
    }

    [Fact]
    public void ForRole_TrimsTheRoleName()
    {
        ApproverResolutionRule.ForRole("  HRManager  ").Value.RoleName.Should().Be("HRManager");
    }

    [Fact]
    public void ForReportingLine_HoldsNoStoredTarget()
    {
        var rule = ApproverResolutionRule.ForReportingLine().Value;

        rule.Kind.Should().Be(ApproverResolutionKind.ReportingLine);
        rule.RoleName.Should().BeNull();
        rule.ScopeId.Should().BeNull();
    }

    [Fact]
    public void ForOrganizationalScope_RequiresBothARoleAndAScope()
    {
        ApproverResolutionRule.ForOrganizationalScope(null, Guid.NewGuid()).Error
            .Should().Be(WorkflowErrors.RoleResolutionRequiresRoleName);
        ApproverResolutionRule.ForOrganizationalScope("HRManager", null).Error
            .Should().Be(WorkflowErrors.ScopeResolutionRequiresScope);
    }

    [Fact]
    public void ForOrganizationalScope_Succeeds_WithBoth()
    {
        var scopeId = Guid.NewGuid();

        var rule = ApproverResolutionRule.ForOrganizationalScope("HRManager", scopeId).Value;

        rule.Kind.Should().Be(ApproverResolutionKind.OrganizationalScope);
        rule.ScopeId.Should().Be(scopeId);
    }

    [Fact]
    public void EqualityIsByValue()
    {
        ApproverResolutionRule.ForRole("HRManager").Value.Should().Be(ApproverResolutionRule.ForRole("HRManager").Value);
        ApproverResolutionRule.ForRole("HRManager").Value.Should().NotBe(ApproverResolutionRule.ForRole("Auditor").Value);
    }

    [Fact]
    public void ToString_DescribesTheRuleKind()
    {
        ApproverResolutionRule.ForRole("HRManager").Value.ToString().Should().Be("Role:HRManager");
        ApproverResolutionRule.ForReportingLine().Value.ToString().Should().Be("ReportingLine");
        ApproverResolutionRule.ForOrganizationalScope("HRManager", Guid.Empty).Value.ToString()
            .Should().StartWith("Scope:HRManager@");
    }

    /// <summary>
    /// WR-010 is structural: the type has no field capable of naming an individual,
    /// so no rule that names a person is constructible at all. This test records that
    /// property rather than exercising a runtime check, because there is none to
    /// exercise.
    /// </summary>
    [Fact]
    public void TheTypeExposesNoWayToNameAnIndividual()
    {
        var properties = typeof(ApproverResolutionRule).GetProperties().Select(p => p.Name).ToList();

        properties.Should().BeEquivalentTo(["Kind", "RoleName", "ScopeId", "CanResolveToEmptySet"]);
    }

    [Fact]
    public void EveryKindCanResolveToAnEmptySet_MakingTheExemptionReachable()
    {
        ApproverResolutionRule.CanResolveToEmptySet.Should().BeTrue();
    }
}

public sealed class SlaDurationTests
{
    [Fact]
    public void Create_Fails_WhenNotPositive()
    {
        SlaDuration.Create(TimeSpan.Zero).Error.Should().Be(WorkflowErrors.SlaDurationMustBePositive);
        SlaDuration.Create(TimeSpan.FromHours(-1)).Error.Should().Be(WorkflowErrors.SlaDurationMustBePositive);
    }

    [Fact]
    public void Create_Fails_WhenBeyondThePlatformMaximum()
    {
        SlaDuration.Create(SlaDuration.PlatformMaximum + TimeSpan.FromDays(1)).Error
            .Should().Be(WorkflowErrors.SlaDurationExceedsPlatformMaximum);
    }

    [Fact]
    public void Create_Succeeds_AtExactlyThePlatformMaximum()
    {
        SlaDuration.Create(SlaDuration.PlatformMaximum).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void EqualityIsByValue()
    {
        SlaDuration.Create(TimeSpan.FromHours(4)).Value.Should().Be(SlaDuration.Create(TimeSpan.FromHours(4)).Value);
    }

    [Fact]
    public void ToString_ShowsTheDuration()
    {
        SlaDuration.Create(TimeSpan.FromHours(4)).Value.ToString().Should().Be(TimeSpan.FromHours(4).ToString());
    }
}

public sealed class EscalationPolicyTests
{
    [Fact]
    public void Create_Fails_WhenTriggerIsNotPositive()
    {
        EscalationPolicy.Create(TimeSpan.Zero, TestWorkflow.RoleRule(), 2).Error
            .Should().Be(WorkflowErrors.EscalationTriggerMustBePositive);
    }

    [Fact]
    public void Create_Fails_WhenTriggerExceedsThePlatformMaximum()
    {
        EscalationPolicy.Create(SlaDuration.PlatformMaximum + TimeSpan.FromDays(1), TestWorkflow.RoleRule(), 2).Error
            .Should().Be(WorkflowErrors.EscalationTriggerExceedsPlatformMaximum);
    }

    [Fact]
    public void Create_Fails_WhenDepthIsBelowOne()
    {
        EscalationPolicy.Create(TimeSpan.FromHours(1), TestWorkflow.RoleRule(), 0).Error
            .Should().Be(WorkflowErrors.EscalationDepthMustBePositive);
    }

    /// <summary>
    /// WR-021. An unbounded chain either escalates forever with nothing surfacing, or
    /// keeps re-resolving until it lands on someone who happens to be able to approve,
    /// turning escalation into an unintended relaxation of who was supposed to decide.
    /// </summary>
    [Fact]
    public void Create_Fails_WhenDepthIsUnbounded()
    {
        EscalationPolicy.Create(TimeSpan.FromHours(1), TestWorkflow.RoleRule(), EscalationPolicy.MaximumDepth + 1).Error
            .Should().Be(WorkflowErrors.EscalationDepthUnbounded);
    }

    [Fact]
    public void Create_Succeeds_AtExactlyTheMaximumDepth()
    {
        EscalationPolicy.Create(TimeSpan.FromHours(1), TestWorkflow.RoleRule(), EscalationPolicy.MaximumDepth)
            .IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_Throws_WhenTargetIsNull()
    {
        var act = () => EscalationPolicy.Create(TimeSpan.FromHours(1), null!, 2);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void EqualityIsByValue()
    {
        var first = EscalationPolicy.Create(TimeSpan.FromHours(1), TestWorkflow.RoleRule(), 2).Value;
        var second = EscalationPolicy.Create(TimeSpan.FromHours(1), TestWorkflow.RoleRule(), 2).Value;

        first.Should().Be(second);
    }
}

public sealed class CommandReferenceTests
{
    [Fact]
    public void Create_Fails_WhenModuleIsMissing()
    {
        CommandReference.Create(" ", "ApproveLeave").Error.Should().Be(WorkflowErrors.CommandReferenceModuleRequired);
    }

    [Fact]
    public void Create_Fails_WhenCommandIsMissing()
    {
        CommandReference.Create("Leave", null).Error.Should().Be(WorkflowErrors.CommandReferenceCommandRequired);
    }

    [Fact]
    public void ToString_QualifiesTheCommandByModule()
    {
        CommandReference.Create("Leave", "ApproveLeave").Value.ToString().Should().Be("Leave.ApproveLeave");
    }

    [Fact]
    public void EqualityIsByValue()
    {
        CommandReference.Create("Leave", "ApproveLeave").Value
            .Should().Be(CommandReference.Create("Leave", "ApproveLeave").Value);
    }

    /// <summary>
    /// WR-001 is structural here too: the type names a module and a command and has
    /// no field able to describe an operation on another module's aggregates.
    /// </summary>
    [Fact]
    public void TheTypeExposesOnlyAModuleAndACommand()
    {
        typeof(CommandReference).GetProperties().Select(p => p.Name)
            .Should().BeEquivalentTo(["ModuleName", "CommandId"]);
    }
}

public sealed class StepConditionTests
{
    [Fact]
    public void Create_Fails_WhenExpressionIsMissing()
    {
        StepCondition.Create("  ", [new ConditionBranch("x", Guid.NewGuid())]).Error
            .Should().Be(WorkflowErrors.StepConditionExpressionRequired);
    }

    [Fact]
    public void Create_Fails_WhenThereAreNoBranches()
    {
        StepCondition.Create("amount", []).Error.Should().Be(WorkflowErrors.StepConditionRequiresBranches);
        StepCondition.Create("amount", null).Error.Should().Be(WorkflowErrors.StepConditionRequiresBranches);
    }

    [Fact]
    public void Create_Fails_WhenABranchHasNoPredicate()
    {
        StepCondition.Create("amount", [new ConditionBranch(" ", Guid.NewGuid())]).Error
            .Should().Be(WorkflowErrors.StepConditionBranchPredicateRequired);
    }

    [Fact]
    public void Create_Succeeds_AndPreservesBranchOrder()
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();

        var condition = StepCondition.Create(
            "amount", [new ConditionBranch("a", first), new ConditionBranch("b", second)]).Value;

        condition.Branches.Select(b => b.TargetStepId).Should().ContainInOrder(first, second);
    }

    [Fact]
    public void EqualityIsByValue()
    {
        var target = Guid.NewGuid();
        var first = StepCondition.Create("amount", [new ConditionBranch("a", target)]).Value;
        var second = StepCondition.Create("amount", [new ConditionBranch("a", target)]).Value;

        first.Should().Be(second);
    }
}
