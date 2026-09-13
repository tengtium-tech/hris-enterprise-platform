using FluentAssertions;
using Hris.Modules.Leave.Domain;
using Xunit;

namespace Hris.Modules.Leave.Tests.Domain;

/// <summary>
/// LV-010, LV-011, LV-012: the versioned, effective-dated rulebook a leave type's
/// requests, accrual, and carryover evaluate against, with <see cref="PolicyAssignment"/>
/// children bound to organizational scope and end-dated rather than deleted.
/// </summary>
public sealed class LeavePolicyTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly LeaveTypeId _leaveTypeId = new(Guid.NewGuid());

    private static LeavePolicyRuleset DefaultRuleset(decimal maximumAccruable = 5) =>
        new(
            AccrualRule: new AccrualRule(AccrualMethod.Periodic, 0.5m, AccrualFrequency.Monthly, true),
            EligibilityCriteria: new EligibilityCriteria(0, [], []),
            EntitlementCap: new EntitlementCap(maximumAccruable, null),
            CarryoverRule: new CarryoverRule(5, 90, CarryoverForfeitureTreatment.Forfeit),
            CommutabilityStatus: new CommutabilityStatus(true, null));

    private LeavePolicy NewPolicy(decimal maximumAccruable = 5) =>
        LeavePolicy.Create(new LeavePolicyId(Guid.NewGuid()), _tenantId, _leaveTypeId, DefaultRuleset(maximumAccruable), Guid.NewGuid(), TestLeave.NowUtc).Value;

    // ---- Create --------------------------------------------------------

    [Fact]
    public void Create_StartsAsDraftAtVersionOne_WithNoEffectiveFromYet()
    {
        var policy = NewPolicy();

        policy.Status.Should().Be(LeavePolicyStatus.Draft);
        policy.Version.Should().Be(1);
        policy.LineageId.Should().Be(policy.Id.Value, "a first version's own id anchors the lineage");
        policy.EffectiveFrom.Should().BeNull("a Draft has not yet been scheduled to take effect");
    }

    // ---- Publish -----------------------------------------------------

    [Fact]
    public void Publish_TransitionsToActive_AndSetsEffectiveFrom()
    {
        var policy = NewPolicy();

        var result = policy.Publish(TestLeave.Today, Guid.NewGuid(), TestLeave.NowUtc);

        result.IsSuccess.Should().BeTrue();
        policy.Status.Should().Be(LeavePolicyStatus.Active);
        policy.EffectiveFrom.Should().Be(TestLeave.Today);
    }

    [Fact]
    public void Publish_Fails_WhenNotDraft()
    {
        var policy = NewPolicy();
        policy.Publish(TestLeave.Today, Guid.NewGuid(), TestLeave.NowUtc);

        var result = policy.Publish(TestLeave.Today, Guid.NewGuid(), TestLeave.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LeaveErrors.PolicyVersionNotDraft);
    }

    // ---- Revise (LV-010) -----------------------------------------------

    [Fact]
    public void Revise_ProducesANewDraftVersion_AndSupersedesTheCurrentOne()
    {
        var policy = NewPolicy();
        policy.Publish(TestLeave.Today, Guid.NewGuid(), TestLeave.NowUtc);
        var newEffectiveFrom = TestLeave.Today.AddMonths(1);

        var result = policy.Revise(
            new LeavePolicyId(Guid.NewGuid()), DefaultRuleset(maximumAccruable: 6), newEffectiveFrom, Guid.NewGuid(), TestLeave.NowUtc);

        result.IsSuccess.Should().BeTrue();
        var next = result.Value;
        next.Version.Should().Be(2);
        next.Status.Should().Be(LeavePolicyStatus.Draft);
        next.LineageId.Should().Be(policy.LineageId, "a revision stays in the same lineage as the version it supersedes");
        policy.Status.Should().Be(LeavePolicyStatus.Superseded);
        policy.EffectiveTo.Should().Be(newEffectiveFrom.AddDays(-1), "the superseded version is end-dated, never deleted");
    }

    [Fact]
    public void Revise_Fails_WhenTheCurrentVersionIsNotActive()
    {
        var policy = NewPolicy(); // still Draft

        var result = policy.Revise(
            new LeavePolicyId(Guid.NewGuid()), DefaultRuleset(), TestLeave.Today.AddMonths(1), Guid.NewGuid(), TestLeave.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LeaveErrors.PolicyVersionNotActive);
    }

    [Fact]
    public void Revise_Fails_WhenTheNewEffectiveDateDoesNotAdvance()
    {
        var policy = NewPolicy();
        policy.Publish(TestLeave.Today, Guid.NewGuid(), TestLeave.NowUtc);

        var result = policy.Revise(
            new LeavePolicyId(Guid.NewGuid()), DefaultRuleset(), TestLeave.Today, Guid.NewGuid(), TestLeave.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LeaveErrors.PolicyRevisionEffectiveDateMustAdvance);
    }

    // ---- Assign / Unassign (LV-012) -------------------------------------

    [Fact]
    public void Assign_Succeeds_AndRaisesAssignedEvent()
    {
        var policy = NewPolicy();
        var assignmentId = new PolicyAssignmentId(Guid.NewGuid());

        var result = policy.Assign(
            assignmentId, LeavePolicyScopeLevel.Department, "dept-1", TestLeave.Today, null, Guid.NewGuid(), TestLeave.NowUtc);

        result.IsSuccess.Should().BeTrue();
        policy.PolicyAssignments.Should().ContainSingle(a => a.Id == assignmentId && a.ScopeTargetId == "dept-1");
        policy.DomainEvents.OfType<LeavePolicyAssigned>().Should().ContainSingle();
    }

    [Fact]
    public void Assign_Fails_WhenItOverlapsAnExistingAssignmentToTheSameScope()
    {
        var policy = NewPolicy();
        policy.Assign(
            new PolicyAssignmentId(Guid.NewGuid()), LeavePolicyScopeLevel.Department, "dept-1", TestLeave.Today, null,
            Guid.NewGuid(), TestLeave.NowUtc);

        var result = policy.Assign(
            new PolicyAssignmentId(Guid.NewGuid()), LeavePolicyScopeLevel.Department, "dept-1", TestLeave.Today.AddDays(10),
            null, Guid.NewGuid(), TestLeave.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LeaveErrors.PolicyAssignmentOverlap);
    }

    [Fact]
    public void Assign_Succeeds_ForADifferentScopeTarget_EvenIfPeriodsOverlap()
    {
        var policy = NewPolicy();
        policy.Assign(
            new PolicyAssignmentId(Guid.NewGuid()), LeavePolicyScopeLevel.Department, "dept-1", TestLeave.Today, null,
            Guid.NewGuid(), TestLeave.NowUtc);

        var result = policy.Assign(
            new PolicyAssignmentId(Guid.NewGuid()), LeavePolicyScopeLevel.Department, "dept-2", TestLeave.Today, null,
            Guid.NewGuid(), TestLeave.NowUtc);

        result.IsSuccess.Should().BeTrue("LV-012 only constrains overlap against the same scope target");
    }

    [Fact]
    public void Assign_Fails_WhenThePolicyIsRetired()
    {
        var policy = NewPolicy();
        policy.Retire();

        var result = policy.Assign(
            new PolicyAssignmentId(Guid.NewGuid()), LeavePolicyScopeLevel.Department, "dept-1", TestLeave.Today, null,
            Guid.NewGuid(), TestLeave.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LeaveErrors.LeavePolicyNotAssignable);
    }

    [Fact]
    public void Unassign_EndDatesTheAssignment_RatherThanRemovingIt()
    {
        var policy = NewPolicy();
        var assignmentId = new PolicyAssignmentId(Guid.NewGuid());
        policy.Assign(assignmentId, LeavePolicyScopeLevel.Department, "dept-1", TestLeave.Today, null, Guid.NewGuid(), TestLeave.NowUtc);
        var endDate = TestLeave.Today.AddDays(5);

        var result = policy.Unassign(assignmentId, endDate);

        result.IsSuccess.Should().BeTrue();
        policy.PolicyAssignments.Should().ContainSingle(a => a.Id == assignmentId && a.EffectiveTo == endDate);
    }

    [Fact]
    public void Unassign_Fails_WhenTheAssignmentDoesNotExist()
    {
        var policy = NewPolicy();

        var result = policy.Unassign(new PolicyAssignmentId(Guid.NewGuid()), TestLeave.Today);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LeaveErrors.PolicyAssignmentNotFound);
    }

    // ---- Retire --------------------------------------------------------

    [Fact]
    public void Retire_TransitionsToRetired()
    {
        var policy = NewPolicy();

        var result = policy.Retire();

        result.IsSuccess.Should().BeTrue();
        policy.Status.Should().Be(LeavePolicyStatus.Retired);
    }

    [Fact]
    public void Retire_IsIdempotent_WhenAlreadyRetired()
    {
        var policy = NewPolicy();
        policy.Retire();

        var result = policy.Retire();

        result.IsSuccess.Should().BeTrue();
        policy.Status.Should().Be(LeavePolicyStatus.Retired);
    }

    // ---- IsEffectiveOn ---------------------------------------------------

    [Fact]
    public void IsEffectiveOn_ReturnsFalse_WhileStillDraft()
    {
        var policy = NewPolicy();

        policy.IsEffectiveOn(TestLeave.Today).Should().BeFalse("a Draft has never been published");
    }

    [Fact]
    public void IsEffectiveOn_ReturnsTrue_WithinAnOpenEndedActivePeriod()
    {
        var policy = NewPolicy();
        policy.Publish(TestLeave.Today, Guid.NewGuid(), TestLeave.NowUtc);

        policy.IsEffectiveOn(TestLeave.Today.AddYears(1)).Should().BeTrue();
    }

    [Fact]
    public void IsEffectiveOn_ReturnsFalse_BeforeTheEffectiveStart()
    {
        var policy = NewPolicy();
        policy.Publish(TestLeave.Today.AddDays(10), Guid.NewGuid(), TestLeave.NowUtc);

        policy.IsEffectiveOn(TestLeave.Today).Should().BeFalse();
    }

    [Fact]
    public void IsEffectiveOn_ReturnsFalse_AfterTheVersionHasBeenSuperseded()
    {
        var policy = NewPolicy();
        policy.Publish(TestLeave.Today, Guid.NewGuid(), TestLeave.NowUtc);
        var newEffectiveFrom = TestLeave.Today.AddDays(30);
        policy.Revise(new LeavePolicyId(Guid.NewGuid()), DefaultRuleset(), newEffectiveFrom, Guid.NewGuid(), TestLeave.NowUtc);

        policy.IsEffectiveOn(newEffectiveFrom).Should().BeFalse("that date now belongs to the next version, and this one is Superseded");
    }
}
