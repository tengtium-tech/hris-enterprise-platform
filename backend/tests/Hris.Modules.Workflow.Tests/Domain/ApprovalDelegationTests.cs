using FluentAssertions;
using Hris.Modules.Workflow.Domain;
using Hris.SharedKernel;
using Xunit;

namespace Hris.Modules.Workflow.Tests.Domain;

public sealed class ApprovalDelegationTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _delegator = Guid.NewGuid();
    private readonly Guid _delegate = Guid.NewGuid();

    private Result<ApprovalDelegation> Create(
        IReadOnlyList<Guid>? scope = null, bool coversAll = true, DateOnly? start = null, DateOnly? end = null,
        string? reason = "Annual leave cover", bool scopeExceeds = false, bool alreadyDelegated = false,
        Guid? delegateAccount = null) =>
        ApprovalDelegation.Create(
            new ApprovalDelegationId(Guid.NewGuid()), _tenantId, _delegator, delegateAccount ?? _delegate, scope,
            coversAll, start ?? TestWorkflow.Today, end ?? TestWorkflow.Today.AddDays(14), reason, null, scopeExceeds,
            alreadyDelegated, Guid.NewGuid(), TestWorkflow.NowUtc);

    [Fact]
    public void Create_StartsScheduled_BecauseScheduledConfersNothing()
    {
        var result = Create();

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(ApprovalDelegationStatus.Scheduled);
        result.Value.DomainEvents.OfType<ApprovalDelegationCreated>().Should().ContainSingle();
    }

    [Fact]
    public void Create_Fails_WhenDelegatorAndDelegateAreTheSameAccount()
    {
        var result = Create(delegateAccount: _delegator);

        result.Error.Should().Be(WorkflowErrors.DelegatorEqualsDelegate);
    }

    [Fact]
    public void Create_Fails_WhenScopeIsEmptyAndItDoesNotCoverAllProcesses()
    {
        Create(scope: [], coversAll: false).Error.Should().Be(WorkflowErrors.DelegationScopeEmpty);
        Create(scope: null, coversAll: false).Error.Should().Be(WorkflowErrors.DelegationScopeEmpty);
    }

    [Fact]
    public void Create_Succeeds_WithAnExplicitProcessScope()
    {
        var process = Guid.NewGuid();

        var result = Create(scope: [process], coversAll: false);

        result.IsSuccess.Should().BeTrue();
        result.Value.Scope.Should().ContainSingle().Which.Should().Be(process);
        result.Value.CoversAllProcesses.Should().BeFalse();
    }

    [Fact]
    public void Create_ClearsScope_WhenItCoversAllProcesses()
    {
        var result = Create(scope: [Guid.NewGuid()], coversAll: true);

        result.Value.CoversAllProcesses.Should().BeTrue();
        result.Value.Scope.Should().BeEmpty("covering everything makes an enumerated scope meaningless");
    }

    /// <summary>WR-041, from a caller-supplied signal.</summary>
    [Fact]
    public void Create_Fails_WhenScopeExceedsTheDelegatorsOwnStanding()
    {
        Create(scopeExceeds: true).Error.Should().Be(WorkflowErrors.DelegationScopeExceedsDelegatorStanding);
    }

    /// <summary>WR-042: chains make the source of approval authority untraceable.</summary>
    [Fact]
    public void Create_Fails_WhenTheAuthorityIsItselfDelegated()
    {
        Create(alreadyDelegated: true).Error.Should().Be(WorkflowErrors.DelegationOfDelegatedAuthority);
    }

    [Fact]
    public void Create_Fails_WhenNoReasonIsGiven()
    {
        Create(reason: "  ").Error.Should().Be(WorkflowErrors.DelegationReasonRequired);
    }

    /// <summary>
    /// WR-040. An unbounded delegation is a standing grant of approval authority and
    /// must be made through administration's role assignment instead. A command shape
    /// with two non-nullable dates cannot express "no end", so what is checkable here
    /// is that an inverted period is rejected.
    /// </summary>
    [Fact]
    public void Create_Fails_WhenThePeriodEndsBeforeItStarts()
    {
        var result = Create(start: TestWorkflow.Today, end: TestWorkflow.Today.AddDays(-1));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SharedKernelErrors.DateRangeEndBeforeStart);
    }

    [Fact]
    public void Activate_MovesToActive_AndRaisesTheEventRoutingActsOn()
    {
        var delegation = Create().Value;

        var result = delegation.Activate(false, TestWorkflow.NowUtc);

        result.IsSuccess.Should().BeTrue();
        delegation.Status.Should().Be(ApprovalDelegationStatus.Active);
        delegation.DomainEvents.OfType<ApprovalDelegationActivated>().Should().ContainSingle();
    }

    /// <summary>
    /// WR-041 is re-validated at activation, not only at creation, because weeks may
    /// pass in between and the delegator's own standing may have changed.
    /// </summary>
    [Fact]
    public void Activate_Fails_WhenTheDelegatorsStandingHasSinceChanged()
    {
        var delegation = Create().Value;

        var result = delegation.Activate(true, TestWorkflow.NowUtc);

        result.Error.Should().Be(WorkflowErrors.DelegationScopeExceedsDelegatorStanding);
        delegation.Status.Should().Be(ApprovalDelegationStatus.Scheduled);
    }

    [Fact]
    public void Activate_Fails_WhenNotScheduled()
    {
        var delegation = Create().Value;
        delegation.Activate(false, TestWorkflow.NowUtc);

        delegation.Activate(false, TestWorkflow.NowUtc).Error.Should().Be(WorkflowErrors.DelegationNotScheduled);
    }

    /// <summary>WR-043: automatic, and carrying no actor because none acted.</summary>
    [Fact]
    public void Expire_EndsTheDelegationWithNoActor()
    {
        var delegation = Create().Value;
        delegation.Activate(false, TestWorkflow.NowUtc);

        var result = delegation.Expire(TestWorkflow.NowUtc);

        result.IsSuccess.Should().BeTrue();
        delegation.Status.Should().Be(ApprovalDelegationStatus.Expired);
        delegation.RevokedBy.Should().BeNull();
        delegation.DomainEvents.OfType<ApprovalDelegationExpired>().Should().ContainSingle();
    }

    [Fact]
    public void Expire_Fails_WhenNotActive()
    {
        var delegation = Create().Value;

        delegation.Expire(TestWorkflow.NowUtc).Error.Should().Be(WorkflowErrors.DelegationNotActive);
    }

    [Fact]
    public void Revoke_EndsItEarly_RecordingActorAndReason()
    {
        var delegation = Create().Value;
        delegation.Activate(false, TestWorkflow.NowUtc);
        var actor = Guid.NewGuid();

        var result = delegation.Revoke(actor, "Returned early", TestWorkflow.NowUtc);

        result.IsSuccess.Should().BeTrue();
        delegation.Status.Should().Be(ApprovalDelegationStatus.Revoked);
        delegation.RevokedBy.Should().Be(actor);
        delegation.RevokedOn.Should().Be(TestWorkflow.NowUtc);
        delegation.DomainEvents.OfType<ApprovalDelegationRevoked>().Should().ContainSingle();
    }

    [Fact]
    public void Revoke_IsIdempotent_OnAnAlreadyTerminalDelegation()
    {
        var delegation = Create().Value;
        delegation.Activate(false, TestWorkflow.NowUtc);
        delegation.Revoke(Guid.NewGuid(), "First", TestWorkflow.NowUtc);

        var result = delegation.Revoke(Guid.NewGuid(), "Second", TestWorkflow.NowUtc);

        result.IsSuccess.Should().BeTrue();
        delegation.DomainEvents.OfType<ApprovalDelegationRevoked>().Should().ContainSingle();
    }

    [Fact]
    public void Revoke_IsIdempotent_OnAnExpiredDelegation()
    {
        var delegation = Create().Value;
        delegation.Activate(false, TestWorkflow.NowUtc);
        delegation.Expire(TestWorkflow.NowUtc);

        delegation.Revoke(Guid.NewGuid(), "Late", TestWorkflow.NowUtc).IsSuccess.Should().BeTrue();
        delegation.Status.Should().Be(ApprovalDelegationStatus.Expired, "an expired delegation stays expired");
    }

    [Fact]
    public void Revoke_CanEndAScheduledDelegationBeforeItBegins()
    {
        var delegation = Create().Value;

        delegation.Revoke(Guid.NewGuid(), "Cancelled", TestWorkflow.NowUtc).IsSuccess.Should().BeTrue();
        delegation.Status.Should().Be(ApprovalDelegationStatus.Revoked);
    }

    [Fact]
    public void CoversProcessOn_RequiresActiveStatus()
    {
        var process = Guid.NewGuid();
        var delegation = Create(scope: [process], coversAll: false).Value;

        delegation.CoversProcessOn(process, TestWorkflow.Today).Should().BeFalse("a scheduled delegation confers nothing");

        delegation.Activate(false, TestWorkflow.NowUtc);
        delegation.CoversProcessOn(process, TestWorkflow.Today).Should().BeTrue();
    }

    [Fact]
    public void CoversProcessOn_RequiresTheDateToFallWithinThePeriod()
    {
        var delegation = Create().Value;
        delegation.Activate(false, TestWorkflow.NowUtc);

        delegation.CoversProcessOn(Guid.NewGuid(), TestWorkflow.Today.AddDays(90)).Should().BeFalse();
    }

    [Fact]
    public void CoversProcessOn_MatchesAnyProcess_WhenItCoversAll()
    {
        var delegation = Create().Value;
        delegation.Activate(false, TestWorkflow.NowUtc);

        delegation.CoversProcessOn(Guid.NewGuid(), TestWorkflow.Today).Should().BeTrue();
    }

    [Fact]
    public void CoversProcessOn_DoesNotMatchAProcessOutsideAnEnumeratedScope()
    {
        var delegation = Create(scope: [Guid.NewGuid()], coversAll: false).Value;
        delegation.Activate(false, TestWorkflow.NowUtc);

        delegation.CoversProcessOn(Guid.NewGuid(), TestWorkflow.Today).Should().BeFalse();
    }
}
