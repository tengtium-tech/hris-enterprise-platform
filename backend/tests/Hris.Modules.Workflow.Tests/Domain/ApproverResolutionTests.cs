using FluentAssertions;
using Hris.Modules.Workflow.Domain;
using Xunit;

namespace Hris.Modules.Workflow.Tests.Domain;

/// <summary>
/// CTR-WFL-002's runtime half. These are the tests that matter most in this module:
/// self-approval prevention is enforced in three separate places, and a hole in any
/// one of them reintroduces the failure the other two exist to prevent.
/// </summary>
public sealed class ApproverResolutionTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _requester = Guid.NewGuid();
    private readonly Guid _process = Guid.NewGuid();

    [Fact]
    public void Resolve_ExcludesTheRequester_WhenTheyAreAmongTheCandidates()
    {
        var otherApprover = Guid.NewGuid();

        var outcome = ApproverResolution.Resolve(
            _requester, [_requester, otherApprover], [], _process, TestWorkflow.Today, false);

        outcome.IsExempt.Should().BeFalse();
        outcome.Approvers.Should().ContainSingle().Which.Should().Be(otherApprover);
    }

    [Fact]
    public void Resolve_ReturnsExempt_WhenExclusionEmptiesTheCandidateSet()
    {
        var outcome = ApproverResolution.Resolve(_requester, [_requester], [], _process, TestWorkflow.Today, false);

        outcome.IsExempt.Should().BeTrue();
        outcome.Approvers.Should().BeEmpty();
    }

    [Fact]
    public void Resolve_ReturnsExempt_WhenNoCandidateExistsAtAll()
    {
        var outcome = ApproverResolution.Resolve(_requester, [], [], _process, TestWorkflow.Today, false);

        outcome.IsExempt.Should().BeTrue();
    }

    /// <summary>
    /// The single-employee-tenant worked example from approval-routing.md: the owner
    /// submits their own leave request, reporting-line resolution finds no manager,
    /// and the instance is exempted rather than self-approved.
    /// </summary>
    [Fact]
    public void Resolve_ExemptsRatherThanSelfApproves_ForTheTopOfHierarchy()
    {
        var owner = Guid.NewGuid();

        var outcome = ApproverResolution.Resolve(owner, [], [], _process, TestWorkflow.Today, false);

        outcome.IsExempt.Should().BeTrue();
        outcome.Approvers.Should().NotContain(owner);
    }

    [Fact]
    public void Resolve_OffersTheStepToADelegate_WhenAnActiveDelegationCoversTheProcess()
    {
        var delegator = Guid.NewGuid();
        var delegateAccount = Guid.NewGuid();
        var delegation = TestWorkflow.ActiveDelegation(_tenantId, delegator, delegateAccount);

        var outcome = ApproverResolution.Resolve(
            _requester, [delegator], [delegation], _process, TestWorkflow.Today, false);

        outcome.IsExempt.Should().BeFalse();
        outcome.Approvers.Should().Contain(delegateAccount);
        outcome.Approvers.Should().Contain(delegator, "delegation changes who may act on a finding, not who resolution found");
    }

    [Fact]
    public void Resolve_DoesNotOfferTheStepToADelegate_WhenTheStepIsNonDelegable()
    {
        var delegator = Guid.NewGuid();
        var delegateAccount = Guid.NewGuid();
        var delegation = TestWorkflow.ActiveDelegation(_tenantId, delegator, delegateAccount);

        var outcome = ApproverResolution.Resolve(
            _requester, [delegator], [delegation], _process, TestWorkflow.Today, true);

        outcome.Approvers.Should().ContainSingle().Which.Should().Be(delegator);
    }

    /// <summary>
    /// WR-011 survives delegation: a delegate who is themself the requester is
    /// excluded exactly as the delegator would have been. Delegation is not a route
    /// around self-approval prevention.
    /// </summary>
    [Fact]
    public void Resolve_ExcludesADelegateWhoIsTheRequester()
    {
        var delegator = Guid.NewGuid();
        var delegation = TestWorkflow.ActiveDelegation(_tenantId, delegator, _requester);

        var outcome = ApproverResolution.Resolve(
            _requester, [delegator], [delegation], _process, TestWorkflow.Today, false);

        outcome.Approvers.Should().NotContain(_requester);
        outcome.Approvers.Should().ContainSingle().Which.Should().Be(delegator);
    }

    [Fact]
    public void Resolve_ReturnsExempt_WhenTheOnlyCandidateIsTheRequesterEvenWithADelegationPresent()
    {
        var delegation = TestWorkflow.ActiveDelegation(_tenantId, Guid.NewGuid(), Guid.NewGuid());

        var outcome = ApproverResolution.Resolve(
            _requester, [_requester], [delegation], _process, TestWorkflow.Today, false);

        outcome.IsExempt.Should().BeTrue();
    }

    [Fact]
    public void Resolve_IgnoresAScheduledDelegation_BecauseScheduledConfersNothing()
    {
        var delegator = Guid.NewGuid();
        var delegateAccount = Guid.NewGuid();
        var scheduled = TestWorkflow.Delegation(_tenantId, delegator, delegateAccount);

        scheduled.Status.Should().Be(ApprovalDelegationStatus.Scheduled);

        var outcome = ApproverResolution.Resolve(
            _requester, [delegator], [scheduled], _process, TestWorkflow.Today, false);

        outcome.Approvers.Should().ContainSingle().Which.Should().Be(delegator);
    }

    [Fact]
    public void Resolve_IgnoresARevokedDelegation()
    {
        var delegator = Guid.NewGuid();
        var delegateAccount = Guid.NewGuid();
        var delegation = TestWorkflow.ActiveDelegation(_tenantId, delegator, delegateAccount);
        delegation.Revoke(Guid.NewGuid(), "No longer needed", TestWorkflow.NowUtc);

        var outcome = ApproverResolution.Resolve(
            _requester, [delegator], [delegation], _process, TestWorkflow.Today, false);

        outcome.Approvers.Should().NotContain(delegateAccount);
    }

    [Fact]
    public void Resolve_IgnoresADelegationOutsideItsPeriod()
    {
        var delegator = Guid.NewGuid();
        var delegateAccount = Guid.NewGuid();
        var delegation = TestWorkflow.ActiveDelegation(_tenantId, delegator, delegateAccount);

        var outcome = ApproverResolution.Resolve(
            _requester, [delegator], [delegation], _process, TestWorkflow.Today.AddDays(60), false);

        outcome.Approvers.Should().NotContain(delegateAccount);
    }

    [Fact]
    public void Resolve_IgnoresADelegationThatDoesNotCoverTheProcess()
    {
        var delegator = Guid.NewGuid();
        var delegateAccount = Guid.NewGuid();
        var delegation = TestWorkflow.ActiveDelegation(
            _tenantId, delegator, delegateAccount, coversAll: false, scope: [Guid.NewGuid()]);

        var outcome = ApproverResolution.Resolve(
            _requester, [delegator], [delegation], _process, TestWorkflow.Today, false);

        outcome.Approvers.Should().NotContain(delegateAccount);
    }

    [Fact]
    public void Resolve_IgnoresADelegationWhoseDelegatorIsNotAmongTheCandidates()
    {
        var unrelatedDelegator = Guid.NewGuid();
        var delegateAccount = Guid.NewGuid();
        var actualApprover = Guid.NewGuid();
        var delegation = TestWorkflow.ActiveDelegation(_tenantId, unrelatedDelegator, delegateAccount);

        var outcome = ApproverResolution.Resolve(
            _requester, [actualApprover], [delegation], _process, TestWorkflow.Today, false);

        outcome.Approvers.Should().ContainSingle().Which.Should().Be(actualApprover);
    }

    [Fact]
    public void Resolve_DeduplicatesCandidates()
    {
        var approver = Guid.NewGuid();

        var outcome = ApproverResolution.Resolve(
            _requester, [approver, approver], [], _process, TestWorkflow.Today, false);

        outcome.Approvers.Should().ContainSingle();
    }

    [Fact]
    public void Resolve_DoesNotAddADelegateTwice_WhenTheyAreAlreadyACandidate()
    {
        var delegator = Guid.NewGuid();
        var delegateAccount = Guid.NewGuid();
        var delegation = TestWorkflow.ActiveDelegation(_tenantId, delegator, delegateAccount);

        var outcome = ApproverResolution.Resolve(
            _requester, [delegator, delegateAccount], [delegation], _process, TestWorkflow.Today, false);

        outcome.Approvers.Should().HaveCount(2);
    }

    [Fact]
    public void Resolve_Throws_WhenCandidateListIsNull()
    {
        var act = () => ApproverResolution.Resolve(_requester, null!, [], _process, TestWorkflow.Today, false);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Resolve_Throws_WhenDelegationListIsNull()
    {
        var act = () => ApproverResolution.Resolve(_requester, [], null!, _process, TestWorkflow.Today, false);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ExemptOutcome_IsDistinguishableFromAResolvedOneWithNoApprovers()
    {
        var exempt = ApproverResolutionOutcome.Exempt;
        var resolved = ApproverResolutionOutcome.Resolved([Guid.NewGuid()]);

        exempt.IsExempt.Should().BeTrue();
        resolved.IsExempt.Should().BeFalse();
    }
}
