using FluentAssertions;
using Hris.Modules.Workflow.Application.Commands;
using Hris.Modules.Workflow.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Modules.Workflow.Tests.Application;

public sealed class CreateApprovalDelegationCommandHandlerTests
{
    private readonly IApprovalDelegationRepository _repository = Substitute.For<IApprovalDelegationRepository>();
    private readonly CreateApprovalDelegationCommandHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _delegator = Guid.NewGuid();
    private readonly Guid _delegate = Guid.NewGuid();

    public CreateApprovalDelegationCommandHandlerTests()
    {
        _handler = new CreateApprovalDelegationCommandHandler(_repository, new FakeTimeProvider(TestWorkflow.NowUtc));
    }

    private CreateApprovalDelegationCommand Command(
        IReadOnlyList<Guid>? scope = null, bool coversAll = true, bool scopeExceeds = false) => new(
        _tenantId, _delegator, _delegate, scope, coversAll, TestWorkflow.Today, TestWorkflow.Today.AddDays(14),
        "Annual leave cover", null, scopeExceeds, Guid.NewGuid());

    [Fact]
    public async Task Handle_CreatesTheDelegation_WhenTheDelegatorHoldsTheAuthorityOutright()
    {
        _repository.ListByDelegateAsync(_tenantId, _delegator, Arg.Any<CancellationToken>()).Returns([]);

        var result = await _handler.Handle(Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).AddAsync(Arg.Any<ApprovalDelegation>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// WR-042. The delegator here holds their approval authority only because someone
    /// else delegated it to them, so they cannot pass it on: A to B to C would leave A
    /// never having consented to C acting.
    /// </summary>
    [Fact]
    public async Task Handle_Fails_WhenTheDelegatorHoldsTheAuthorityOnlyByDelegation()
    {
        var inbound = TestWorkflow.ActiveDelegation(_tenantId, Guid.NewGuid(), _delegator);
        _repository.ListByDelegateAsync(_tenantId, _delegator, Arg.Any<CancellationToken>()).Returns([inbound]);

        var result = await _handler.Handle(Command(), CancellationToken.None);

        result.Error.Should().Be(WorkflowErrors.DelegationOfDelegatedAuthority);
    }

    [Fact]
    public async Task Handle_Succeeds_WhenTheInboundDelegationIsStillScheduled()
    {
        var inbound = TestWorkflow.Delegation(_tenantId, Guid.NewGuid(), _delegator);
        _repository.ListByDelegateAsync(_tenantId, _delegator, Arg.Any<CancellationToken>()).Returns([inbound]);

        var result = await _handler.Handle(Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue("a scheduled delegation confers nothing yet, so nothing is being passed on");
    }

    [Fact]
    public async Task Handle_Succeeds_WhenTheInboundDelegationCoversUnrelatedProcesses()
    {
        var inbound = TestWorkflow.ActiveDelegation(
            _tenantId, Guid.NewGuid(), _delegator, coversAll: false, scope: [Guid.NewGuid()]);
        _repository.ListByDelegateAsync(_tenantId, _delegator, Arg.Any<CancellationToken>()).Returns([inbound]);

        var result = await _handler.Handle(Command(scope: [Guid.NewGuid()], coversAll: false), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Fails_WhenTheInboundDelegationOverlapsTheRequestedScope()
    {
        var shared = Guid.NewGuid();
        var inbound = TestWorkflow.ActiveDelegation(_tenantId, Guid.NewGuid(), _delegator, coversAll: false, scope: [shared]);
        _repository.ListByDelegateAsync(_tenantId, _delegator, Arg.Any<CancellationToken>()).Returns([inbound]);

        var result = await _handler.Handle(Command(scope: [shared], coversAll: false), CancellationToken.None);

        result.Error.Should().Be(WorkflowErrors.DelegationOfDelegatedAuthority);
    }

    [Fact]
    public async Task Handle_Fails_WhenTheScopeExceedsTheDelegatorsStanding()
    {
        _repository.ListByDelegateAsync(_tenantId, _delegator, Arg.Any<CancellationToken>()).Returns([]);

        var result = await _handler.Handle(Command(scopeExceeds: true), CancellationToken.None);

        result.Error.Should().Be(WorkflowErrors.DelegationScopeExceedsDelegatorStanding);
    }
}

public sealed class ActivateApprovalDelegationCommandHandlerTests
{
    private readonly IApprovalDelegationRepository _repository = Substitute.For<IApprovalDelegationRepository>();
    private readonly ActivateApprovalDelegationCommandHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public ActivateApprovalDelegationCommandHandlerTests()
    {
        _handler = new ActivateApprovalDelegationCommandHandler(_repository, new FakeTimeProvider(TestWorkflow.NowUtc));
    }

    [Fact]
    public async Task Handle_Activates_AScheduledDelegation()
    {
        var delegation = TestWorkflow.Delegation(_tenantId, Guid.NewGuid(), Guid.NewGuid());
        _repository.GetByIdAsync(delegation.Id, Arg.Any<CancellationToken>()).Returns(delegation);

        var result = await _handler.Handle(
            new ActivateApprovalDelegationCommand(_tenantId, delegation.Id.Value, false), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        delegation.Status.Should().Be(ApprovalDelegationStatus.Active);
    }

    [Fact]
    public async Task Handle_Fails_WhenStandingChangedBetweenSchedulingAndActivation()
    {
        var delegation = TestWorkflow.Delegation(_tenantId, Guid.NewGuid(), Guid.NewGuid());
        _repository.GetByIdAsync(delegation.Id, Arg.Any<CancellationToken>()).Returns(delegation);

        var result = await _handler.Handle(
            new ActivateApprovalDelegationCommand(_tenantId, delegation.Id.Value, true), CancellationToken.None);

        result.Error.Should().Be(WorkflowErrors.DelegationScopeExceedsDelegatorStanding);
    }

    [Fact]
    public async Task Handle_Fails_WhenTheDelegationIsNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<ApprovalDelegationId>(), Arg.Any<CancellationToken>())
            .Returns((ApprovalDelegation?)null);

        var result = await _handler.Handle(
            new ActivateApprovalDelegationCommand(_tenantId, Guid.NewGuid(), false), CancellationToken.None);

        result.Error.Should().Be(WorkflowErrors.DelegationNotFound);
    }

    [Fact]
    public async Task Handle_ReturnsNotFound_ForADelegationInAnotherTenant()
    {
        var delegation = TestWorkflow.Delegation(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        _repository.GetByIdAsync(delegation.Id, Arg.Any<CancellationToken>()).Returns(delegation);

        var result = await _handler.Handle(
            new ActivateApprovalDelegationCommand(_tenantId, delegation.Id.Value, false), CancellationToken.None);

        result.Error.Should().Be(WorkflowErrors.DelegationNotFound);
    }
}

public sealed class ExpireApprovalDelegationCommandHandlerTests
{
    private readonly IApprovalDelegationRepository _repository = Substitute.For<IApprovalDelegationRepository>();
    private readonly ExpireApprovalDelegationCommandHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public ExpireApprovalDelegationCommandHandlerTests()
    {
        _handler = new ExpireApprovalDelegationCommandHandler(_repository, new FakeTimeProvider(TestWorkflow.NowUtc));
    }

    [Fact]
    public async Task Handle_Expires_AnActiveDelegation()
    {
        var delegation = TestWorkflow.ActiveDelegation(_tenantId, Guid.NewGuid(), Guid.NewGuid());
        _repository.GetByIdAsync(delegation.Id, Arg.Any<CancellationToken>()).Returns(delegation);

        var result = await _handler.Handle(
            new ExpireApprovalDelegationCommand(_tenantId, delegation.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        delegation.Status.Should().Be(ApprovalDelegationStatus.Expired);
    }

    [Fact]
    public async Task Handle_Fails_WhenTheDelegationIsNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<ApprovalDelegationId>(), Arg.Any<CancellationToken>())
            .Returns((ApprovalDelegation?)null);

        var result = await _handler.Handle(
            new ExpireApprovalDelegationCommand(_tenantId, Guid.NewGuid()), CancellationToken.None);

        result.Error.Should().Be(WorkflowErrors.DelegationNotFound);
    }
}

public sealed class RevokeApprovalDelegationCommandHandlerTests
{
    private readonly IApprovalDelegationRepository _repository = Substitute.For<IApprovalDelegationRepository>();
    private readonly RevokeApprovalDelegationCommandHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public RevokeApprovalDelegationCommandHandlerTests()
    {
        _handler = new RevokeApprovalDelegationCommandHandler(_repository, new FakeTimeProvider(TestWorkflow.NowUtc));
    }

    [Fact]
    public async Task Handle_Revokes_AnActiveDelegation()
    {
        var delegation = TestWorkflow.ActiveDelegation(_tenantId, Guid.NewGuid(), Guid.NewGuid());
        _repository.GetByIdAsync(delegation.Id, Arg.Any<CancellationToken>()).Returns(delegation);

        var result = await _handler.Handle(
            new RevokeApprovalDelegationCommand(_tenantId, delegation.Id.Value, Guid.NewGuid(), "Returned early"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        delegation.Status.Should().Be(ApprovalDelegationStatus.Revoked);
    }

    [Fact]
    public async Task Handle_Fails_WhenTheDelegationIsNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<ApprovalDelegationId>(), Arg.Any<CancellationToken>())
            .Returns((ApprovalDelegation?)null);

        var result = await _handler.Handle(
            new RevokeApprovalDelegationCommand(_tenantId, Guid.NewGuid(), Guid.NewGuid(), "Reason"), CancellationToken.None);

        result.Error.Should().Be(WorkflowErrors.DelegationNotFound);
    }
}
