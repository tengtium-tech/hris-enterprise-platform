using FluentAssertions;
using Hris.Modules.Workflow.Application.Mapping;
using Hris.Modules.Workflow.Domain;
using Xunit;

namespace Hris.Modules.Workflow.Tests.Application;

public sealed class WorkflowMapperTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void ToDto_ProjectsEveryDefinitionField()
    {
        var definition = TestWorkflow.Published(_tenantId);

        var dto = WorkflowMapper.ToDto(definition);

        dto.Id.Should().Be(definition.Id.Value);
        dto.TenantId.Should().Be(_tenantId);
        dto.LineageId.Should().Be(definition.LineageId);
        dto.Status.Should().Be("Published");
        dto.Version.Should().Be(1);
        dto.Steps.Should().HaveCount(2);
        dto.PublishedOn.Should().Be(TestWorkflow.NowUtc);
    }

    [Fact]
    public void ToDto_ProjectsDeprecationFields()
    {
        var definition = TestWorkflow.Published(_tenantId);
        var actor = Guid.NewGuid();
        definition.Deprecate(actor, "Superseded", TestWorkflow.NowUtc);

        var dto = WorkflowMapper.ToDto(definition);

        dto.Status.Should().Be("Deprecated");
        dto.DeprecatedBy.Should().Be(actor);
        dto.DeprecationReason.Should().Be("Superseded");
    }

    [Fact]
    public void ToDto_ProjectsAnApprovalStepWithOverrides()
    {
        var step = WorkflowStep.Create(
            new WorkflowStepId(Guid.NewGuid()), 1, StepType.Approval, "Approve", TestWorkflow.RoleRule("HRManager"), null,
            null, [Guid.NewGuid()], null, null, true, TestWorkflow.Escalation(3), TestWorkflow.Sla(72)).Value;

        var dto = WorkflowMapper.ToDto(step);

        dto.StepType.Should().Be("Approval");
        dto.NonDelegable.Should().BeTrue();
        dto.ApproverResolutionRule!.Kind.Should().Be("Role");
        dto.ApproverResolutionRule.RoleName.Should().Be("HRManager");
        dto.EscalationOverride!.MaxDepth.Should().Be(3);
        dto.SlaOverride.Should().Be(TimeSpan.FromHours(72));
    }

    [Fact]
    public void ToDto_ProjectsAnAutomatedStepsCommandReference()
    {
        var command = CommandReference.Create("Leave", "ApproveLeave").Value;
        var step = WorkflowStep.Create(
            new WorkflowStepId(Guid.NewGuid()), 1, StepType.Automated, "Run", null, command, null, [Guid.NewGuid()], null,
            null, false, null, null).Value;

        var dto = WorkflowMapper.ToDto(step);

        dto.CommandReference!.ModuleName.Should().Be("Leave");
        dto.CommandReference.CommandId.Should().Be("ApproveLeave");
    }

    [Fact]
    public void ToDto_ProjectsAConditionalStepsBranches()
    {
        var branchTarget = Guid.NewGuid();
        var step = TestWorkflow.ConditionalStep(Guid.NewGuid(), 1, branchTarget, Guid.NewGuid());

        var dto = WorkflowMapper.ToDto(step);

        dto.Condition!.Expression.Should().Be("amount");
        dto.Condition.Branches.Should().ContainSingle().Which.TargetStepId.Should().Be(branchTarget);
        dto.DefaultSuccessor.Should().NotBeNull();
    }

    [Fact]
    public void ToDto_ProjectsAJoinMode_WhenPresent()
    {
        var step = WorkflowStep.Create(
            new WorkflowStepId(Guid.NewGuid()), 1, StepType.Approval, "Join", TestWorkflow.ReportingLineRule, null, null,
            [Guid.NewGuid()], null, JoinMode.All, false, null, null).Value;

        WorkflowMapper.ToDto(step).JoinMode.Should().Be("All");
    }

    [Fact]
    public void ToDto_LeavesOptionalStepFieldsNull_WhenAbsent()
    {
        var dto = WorkflowMapper.ToDto(TestWorkflow.TerminalStep(Guid.NewGuid(), 1));

        dto.ApproverResolutionRule.Should().BeNull();
        dto.CommandReference.Should().BeNull();
        dto.Condition.Should().BeNull();
        dto.EscalationOverride.Should().BeNull();
        dto.SlaOverride.Should().BeNull();
        dto.JoinMode.Should().BeNull();
    }

    [Fact]
    public void ToDto_ProjectsAConfiguredPolicy()
    {
        var policy = TestWorkflow.Policy(_tenantId);
        var process = Guid.NewGuid();
        policy.Configure(
            [process], TestWorkflow.Escalation(), TestWorkflow.Sla(), [new ApprovalAuthorityLimit("HRManager", null, 5000m)],
            true, false, Guid.NewGuid(), "Reason", TestWorkflow.NowUtc);

        var dto = WorkflowMapper.ToDto(policy);

        dto.ProcessesRequiringApproval.Should().ContainSingle().Which.Should().Be(process);
        dto.DefaultEscalation.Should().NotBeNull();
        dto.DefaultSla.Should().NotBeNull();
        dto.AuthorityLimits.Should().ContainSingle().Which.MaximumAmount.Should().Be(5000m);
        dto.CustomDefinitionsPermitted.Should().BeTrue();
    }

    [Fact]
    public void ToDto_ProjectsAnUnconfiguredPolicyWithNullDefaults()
    {
        var dto = WorkflowMapper.ToDto(TestWorkflow.Policy(_tenantId));

        dto.DefaultEscalation.Should().BeNull();
        dto.DefaultSla.Should().BeNull();
        dto.LastConfiguredBy.Should().BeNull();
    }

    [Fact]
    public void ToDto_ProjectsADelegationCarryingBothIdentities()
    {
        var delegator = Guid.NewGuid();
        var delegateAccount = Guid.NewGuid();
        var delegation = TestWorkflow.Delegation(_tenantId, delegator, delegateAccount);

        var dto = WorkflowMapper.ToDto(delegation);

        dto.DelegatorUserAccountId.Should().Be(delegator);
        dto.DelegateUserAccountId.Should().Be(delegateAccount);
        dto.Status.Should().Be("Scheduled");
        dto.CoversAllProcesses.Should().BeTrue();
        dto.PeriodStart.Should().Be(delegation.Period.Start);
        dto.PeriodEnd.Should().Be(delegation.Period.End);
    }

    [Fact]
    public void ToDto_ProjectsRevocationFields()
    {
        var delegation = TestWorkflow.ActiveDelegation(_tenantId, Guid.NewGuid(), Guid.NewGuid());
        var actor = Guid.NewGuid();
        delegation.Revoke(actor, "Returned", TestWorkflow.NowUtc);

        var dto = WorkflowMapper.ToDto(delegation);

        dto.RevokedBy.Should().Be(actor);
        dto.RevokedOn.Should().Be(TestWorkflow.NowUtc);
    }

    [Fact]
    public void ToDto_Throws_OnNullInput()
    {
        ((Action)(() => WorkflowMapper.ToDto((WorkflowDefinition)null!))).Should().Throw<ArgumentNullException>();
        ((Action)(() => WorkflowMapper.ToDto((WorkflowStep)null!))).Should().Throw<ArgumentNullException>();
        ((Action)(() => WorkflowMapper.ToDto((ApprovalPolicy)null!))).Should().Throw<ArgumentNullException>();
        ((Action)(() => WorkflowMapper.ToDto((ApprovalDelegation)null!))).Should().Throw<ArgumentNullException>();
        ((Action)(() => WorkflowMapper.ToDto((ApproverResolutionRule)null!))).Should().Throw<ArgumentNullException>();
        ((Action)(() => WorkflowMapper.ToDto((StepCondition)null!))).Should().Throw<ArgumentNullException>();
        ((Action)(() => WorkflowMapper.ToDto((EscalationPolicy)null!))).Should().Throw<ArgumentNullException>();
    }
}
