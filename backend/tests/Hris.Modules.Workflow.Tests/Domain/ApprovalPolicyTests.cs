using FluentAssertions;
using Hris.Modules.Workflow.Domain;
using Xunit;

namespace Hris.Modules.Workflow.Tests.Domain;

public sealed class ApprovalPolicyTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void Create_Succeeds_WhenNoPolicyExistsForTheTenant()
    {
        var result = ApprovalPolicy.Create(new ApprovalPolicyId(Guid.NewGuid()), _tenantId, false, TestWorkflow.NowUtc);

        result.IsSuccess.Should().BeTrue();
        result.Value.TenantId.Should().Be(_tenantId);
        result.Value.ProcessesRequiringApproval.Should().BeEmpty();
    }

    /// <summary>WR-030: exactly one policy per tenant.</summary>
    [Fact]
    public void Create_Fails_WhenAPolicyAlreadyExistsForTheTenant()
    {
        var result = ApprovalPolicy.Create(new ApprovalPolicyId(Guid.NewGuid()), _tenantId, true, TestWorkflow.NowUtc);

        result.Error.Should().Be(WorkflowErrors.ApprovalPolicyAlreadyExistsForTenant);
    }

    [Fact]
    public void Configure_Fails_WhenNoReasonIsGiven()
    {
        var policy = TestWorkflow.Policy(_tenantId);

        var result = policy.Configure(null, null, null, null, true, false, Guid.NewGuid(), "  ", TestWorkflow.NowUtc);

        result.Error.Should().Be(WorkflowErrors.PolicyChangeReasonRequired);
    }

    /// <summary>WR-031, from a caller-supplied signal.</summary>
    [Fact]
    public void Configure_Fails_WhenAnAuthorityLimitExceedsRoleStanding()
    {
        var policy = TestWorkflow.Policy(_tenantId);

        var result = policy.Configure(
            null, null, null, [new ApprovalAuthorityLimit("HRManager", null, 100m)], true, true, Guid.NewGuid(),
            "Raising limits", TestWorkflow.NowUtc);

        result.Error.Should().Be(WorkflowErrors.AuthorityLimitExceedsRoleStanding);
    }

    [Fact]
    public void Configure_StoresTheWholeSurface_AndRaisesAnEvent()
    {
        var policy = TestWorkflow.Policy(_tenantId);
        var process = Guid.NewGuid();
        var actor = Guid.NewGuid();

        var result = policy.Configure(
            [process], TestWorkflow.Escalation(), TestWorkflow.Sla(), [new ApprovalAuthorityLimit("HRManager", null, 5000m)],
            true, false, actor, "Initial governance", TestWorkflow.NowUtc);

        result.IsSuccess.Should().BeTrue();
        policy.ProcessesRequiringApproval.Should().ContainSingle().Which.Should().Be(process);
        policy.DefaultEscalation.Should().NotBeNull();
        policy.DefaultSla.Should().NotBeNull();
        policy.AuthorityLimits.Should().ContainSingle();
        policy.CustomDefinitionsPermitted.Should().BeTrue();
        policy.LastConfiguredBy.Should().Be(actor);
        policy.LastConfigurationReason.Should().Be("Initial governance");
        policy.DomainEvents.OfType<ApprovalPolicyConfigured>().Should().ContainSingle();
    }

    [Fact]
    public void Configure_DeduplicatesTheProcessList()
    {
        var policy = TestWorkflow.Policy(_tenantId);
        var process = Guid.NewGuid();

        policy.Configure([process, process], null, null, null, false, false, Guid.NewGuid(), "Reason", TestWorkflow.NowUtc);

        policy.ProcessesRequiringApproval.Should().ContainSingle();
    }

    [Fact]
    public void Configure_TreatsNullCollectionsAsEmpty()
    {
        var policy = TestWorkflow.Policy(_tenantId);

        policy.Configure(null, null, null, null, false, false, Guid.NewGuid(), "Reason", TestWorkflow.NowUtc);

        policy.ProcessesRequiringApproval.Should().BeEmpty();
        policy.AuthorityLimits.Should().BeEmpty();
    }

    [Fact]
    public void RequiresApproval_ReflectsTheConfiguredProcessList()
    {
        var policy = TestWorkflow.Policy(_tenantId);
        var gated = Guid.NewGuid();

        policy.Configure([gated], null, null, null, false, false, Guid.NewGuid(), "Reason", TestWorkflow.NowUtc);

        policy.RequiresApproval(gated).Should().BeTrue();
        policy.RequiresApproval(Guid.NewGuid()).Should().BeFalse(
            "a process the tenant did not gate simply has no definition, which is a valid common state");
    }

    /// <summary>
    /// WR-032. The absence of a self-approval setting is the point, not an omission
    /// to be filled in later, so it is asserted rather than left implicit.
    /// </summary>
    [Fact]
    public void TheAggregateExposesNoSelfApprovalSetting()
    {
        var propertyNames = typeof(ApprovalPolicy).GetProperties().Select(p => p.Name).ToList();

        propertyNames.Should().NotContain(name => name.Contains("SelfApprov", StringComparison.OrdinalIgnoreCase));
    }
}
