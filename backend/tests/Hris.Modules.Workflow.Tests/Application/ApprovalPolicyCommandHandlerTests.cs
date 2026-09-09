using FluentAssertions;
using Hris.Modules.Workflow.Application.Commands;
using Hris.Modules.Workflow.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Modules.Workflow.Tests.Application;

public sealed class CreateApprovalPolicyCommandHandlerTests
{
    private readonly IApprovalPolicyRepository _repository = Substitute.For<IApprovalPolicyRepository>();
    private readonly CreateApprovalPolicyCommandHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public CreateApprovalPolicyCommandHandlerTests()
    {
        _handler = new CreateApprovalPolicyCommandHandler(_repository, new FakeTimeProvider(TestWorkflow.NowUtc));
    }

    [Fact]
    public async Task Handle_CreatesThePolicy_WhenTheTenantHasNone()
    {
        _repository.GetByTenantAsync(_tenantId, Arg.Any<CancellationToken>()).Returns((ApprovalPolicy?)null);

        var result = await _handler.Handle(new CreateApprovalPolicyCommand(_tenantId, Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).AddAsync(Arg.Any<ApprovalPolicy>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_WhenTheTenantAlreadyHasAPolicy()
    {
        _repository.GetByTenantAsync(_tenantId, Arg.Any<CancellationToken>()).Returns(TestWorkflow.Policy(_tenantId));

        var result = await _handler.Handle(new CreateApprovalPolicyCommand(_tenantId, Guid.NewGuid()), CancellationToken.None);

        result.Error.Should().Be(WorkflowErrors.ApprovalPolicyAlreadyExistsForTenant);
        await _repository.DidNotReceive().AddAsync(Arg.Any<ApprovalPolicy>(), Arg.Any<CancellationToken>());
    }
}

public sealed class ConfigureApprovalPolicyCommandHandlerTests
{
    private readonly IApprovalPolicyRepository _repository = Substitute.For<IApprovalPolicyRepository>();
    private readonly ConfigureApprovalPolicyCommandHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public ConfigureApprovalPolicyCommandHandlerTests()
    {
        _handler = new ConfigureApprovalPolicyCommandHandler(_repository, new FakeTimeProvider(TestWorkflow.NowUtc));
    }

    private ConfigureApprovalPolicyCommand Command(
        TimeSpan? escalationTrigger = null, ApproverResolutionKind? escalationKind = null, string? escalationRole = null,
        int? maxDepth = null, TimeSpan? sla = null, string? reason = "Governance change") => new(
        _tenantId, null, escalationTrigger, escalationKind, escalationRole, null, maxDepth, sla, null, true, false,
        Guid.NewGuid(), reason);

    [Fact]
    public async Task Handle_Fails_WhenNoPolicyExists()
    {
        _repository.GetByTenantAsync(_tenantId, Arg.Any<CancellationToken>()).Returns((ApprovalPolicy?)null);

        var result = await _handler.Handle(Command(), CancellationToken.None);

        result.Error.Should().Be(WorkflowErrors.ApprovalPolicyNotFound);
    }

    [Fact]
    public async Task Handle_ConfiguresThePolicy_WithNoOptionalDefaults()
    {
        var policy = TestWorkflow.Policy(_tenantId);
        _repository.GetByTenantAsync(_tenantId, Arg.Any<CancellationToken>()).Returns(policy);

        var result = await _handler.Handle(Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        policy.DefaultEscalation.Should().BeNull();
        policy.DefaultSla.Should().BeNull();
    }

    [Fact]
    public async Task Handle_BuildsTheDefaultEscalationPolicy_FromTheCommandFields()
    {
        var policy = TestWorkflow.Policy(_tenantId);
        _repository.GetByTenantAsync(_tenantId, Arg.Any<CancellationToken>()).Returns(policy);

        var result = await _handler.Handle(
            Command(TimeSpan.FromHours(24), ApproverResolutionKind.Role, "HRManager", 3), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        policy.DefaultEscalation!.MaxDepth.Should().Be(3);
        policy.DefaultEscalation.EscalateTo.RoleName.Should().Be("HRManager");
    }

    [Fact]
    public async Task Handle_BuildsAReportingLineEscalationTarget()
    {
        var policy = TestWorkflow.Policy(_tenantId);
        _repository.GetByTenantAsync(_tenantId, Arg.Any<CancellationToken>()).Returns(policy);

        var result = await _handler.Handle(
            Command(TimeSpan.FromHours(24), ApproverResolutionKind.ReportingLine, null, 2), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        policy.DefaultEscalation!.EscalateTo.Kind.Should().Be(ApproverResolutionKind.ReportingLine);
    }

    [Fact]
    public async Task Handle_Fails_WhenAnEscalationTriggerIsGivenWithNoTargetKind()
    {
        var policy = TestWorkflow.Policy(_tenantId);
        _repository.GetByTenantAsync(_tenantId, Arg.Any<CancellationToken>()).Returns(policy);

        var result = await _handler.Handle(Command(TimeSpan.FromHours(24)), CancellationToken.None);

        result.Error.Should().Be(WorkflowErrors.RoleResolutionRequiresRoleName);
    }

    [Fact]
    public async Task Handle_Fails_WhenTheEscalationTargetRoleIsMissing()
    {
        var policy = TestWorkflow.Policy(_tenantId);
        _repository.GetByTenantAsync(_tenantId, Arg.Any<CancellationToken>()).Returns(policy);

        var result = await _handler.Handle(
            Command(TimeSpan.FromHours(24), ApproverResolutionKind.Role, null, 2), CancellationToken.None);

        result.Error.Should().Be(WorkflowErrors.RoleResolutionRequiresRoleName);
    }

    [Fact]
    public async Task Handle_Fails_WhenTheEscalationDepthIsUnbounded()
    {
        var policy = TestWorkflow.Policy(_tenantId);
        _repository.GetByTenantAsync(_tenantId, Arg.Any<CancellationToken>()).Returns(policy);

        var result = await _handler.Handle(
            Command(TimeSpan.FromHours(24), ApproverResolutionKind.Role, "HRManager", EscalationPolicy.MaximumDepth + 1),
            CancellationToken.None);

        result.Error.Should().Be(WorkflowErrors.EscalationDepthUnbounded);
    }

    [Fact]
    public async Task Handle_BuildsTheDefaultSla()
    {
        var policy = TestWorkflow.Policy(_tenantId);
        _repository.GetByTenantAsync(_tenantId, Arg.Any<CancellationToken>()).Returns(policy);

        var result = await _handler.Handle(Command(sla: TimeSpan.FromHours(72)), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        policy.DefaultSla!.Value.Should().Be(TimeSpan.FromHours(72));
    }

    [Fact]
    public async Task Handle_Fails_WhenTheDefaultSlaIsNotPositive()
    {
        var policy = TestWorkflow.Policy(_tenantId);
        _repository.GetByTenantAsync(_tenantId, Arg.Any<CancellationToken>()).Returns(policy);

        var result = await _handler.Handle(Command(sla: TimeSpan.Zero), CancellationToken.None);

        result.Error.Should().Be(WorkflowErrors.SlaDurationMustBePositive);
    }

    [Fact]
    public async Task Handle_Fails_WhenNoReasonIsGiven()
    {
        var policy = TestWorkflow.Policy(_tenantId);
        _repository.GetByTenantAsync(_tenantId, Arg.Any<CancellationToken>()).Returns(policy);

        var result = await _handler.Handle(Command(reason: "  "), CancellationToken.None);

        result.Error.Should().Be(WorkflowErrors.PolicyChangeReasonRequired);
    }
}
