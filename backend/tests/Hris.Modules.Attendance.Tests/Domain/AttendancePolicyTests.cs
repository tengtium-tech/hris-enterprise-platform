using FluentAssertions;
using Hris.Modules.Attendance.Domain;
using Xunit;

namespace Hris.Modules.Attendance.Tests.Domain;

/// <summary>
/// AT-002, AT-011: the configurable rulebook the calculation engine evaluates every
/// record against, authored as effective-dated versions never edited in place, with
/// <see cref="PolicyAssignment"/> children bound to organizational scope and end-dated
/// rather than deleted.
/// </summary>
public sealed class AttendancePolicyTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    private AttendancePolicy NewPolicy(DateOnly? effectiveFrom = null) =>
        AttendancePolicy.Create(
            new AttendancePolicyId(Guid.NewGuid()), _tenantId, "Standard Policy", TestAttendance.DefaultPolicy(),
            effectiveFrom ?? TestAttendance.Today, Guid.NewGuid(), TestAttendance.NowUtc).Value;

    // ---- Create --------------------------------------------------------

    [Fact]
    public void Create_StartsAsDraftAtVersionOne_AndRaisesDefinedEvent()
    {
        var policy = NewPolicy();

        policy.Status.Should().Be(AttendancePolicyStatus.Draft);
        policy.Version.Should().Be(1);
        policy.LineageId.Should().Be(policy.Id.Value, "a first version's own id anchors the lineage");
        policy.DomainEvents.OfType<AttendancePolicyDefined>().Should().ContainSingle();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_Fails_WhenNameIsMissing(string? name)
    {
        var result = AttendancePolicy.Create(
            new AttendancePolicyId(Guid.NewGuid()), _tenantId, name, TestAttendance.DefaultPolicy(),
            TestAttendance.Today, Guid.NewGuid(), TestAttendance.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AttendanceErrors.PolicyNameRequired);
    }

    // ---- Publish -----------------------------------------------------

    [Fact]
    public void Publish_TransitionsToActive_FromDraft()
    {
        var policy = NewPolicy();

        var result = policy.Publish(TestAttendance.Today, Guid.NewGuid(), TestAttendance.NowUtc);

        result.IsSuccess.Should().BeTrue();
        policy.Status.Should().Be(AttendancePolicyStatus.Active);
    }

    [Fact]
    public void Publish_Fails_WhenNotDraft()
    {
        var policy = NewPolicy();
        policy.Publish(TestAttendance.Today, Guid.NewGuid(), TestAttendance.NowUtc);

        var result = policy.Publish(TestAttendance.Today, Guid.NewGuid(), TestAttendance.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AttendanceErrors.PolicyVersionNotDraft);
    }

    // ---- Revise (AT-002) -----------------------------------------------

    [Fact]
    public void Revise_ProducesANewDraftVersion_AndEndDatesTheCurrentOne()
    {
        var policy = NewPolicy();
        policy.Publish(TestAttendance.Today, Guid.NewGuid(), TestAttendance.NowUtc);
        var newEffectiveFrom = TestAttendance.Today.AddMonths(1);

        var result = policy.Revise(
            new AttendancePolicyId(Guid.NewGuid()), TestAttendance.DefaultPolicy(overtimeEligible: false),
            newEffectiveFrom, Guid.NewGuid(), TestAttendance.NowUtc);

        result.IsSuccess.Should().BeTrue();
        var next = result.Value;
        next.Version.Should().Be(2);
        next.Status.Should().Be(AttendancePolicyStatus.Draft);
        next.LineageId.Should().Be(policy.LineageId, "a revision stays in the same lineage as the version it supersedes");
        policy.EffectiveTo.Should().Be(newEffectiveFrom.AddDays(-1), "the superseded version is end-dated, never deleted");
    }

    [Fact]
    public void Revise_Fails_WhenTheCurrentVersionIsNotActive()
    {
        var policy = NewPolicy(); // still Draft

        var result = policy.Revise(
            new AttendancePolicyId(Guid.NewGuid()), TestAttendance.DefaultPolicy(), TestAttendance.Today.AddMonths(1),
            Guid.NewGuid(), TestAttendance.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AttendanceErrors.PolicyVersionNotDraft);
    }

    [Fact]
    public void Revise_Fails_WhenTheNewEffectiveDateDoesNotAdvance()
    {
        // Regression: this case once returned PolicyVersionNotDraft, the same code as
        // "the current version isn't Active" -- a caller could not tell the two failures
        // apart. It now has its own error.
        var policy = NewPolicy();
        policy.Publish(TestAttendance.Today, Guid.NewGuid(), TestAttendance.NowUtc);

        var result = policy.Revise(
            new AttendancePolicyId(Guid.NewGuid()), TestAttendance.DefaultPolicy(), TestAttendance.Today,
            Guid.NewGuid(), TestAttendance.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AttendanceErrors.PolicyRevisionEffectiveDateMustAdvance);
    }

    // ---- Assign / Unassign (AT-011) -------------------------------------

    [Fact]
    public void Assign_Succeeds_AndRaisesAssignedEvent()
    {
        var policy = NewPolicy();
        var assignmentId = new PolicyAssignmentId(Guid.NewGuid());

        var result = policy.Assign(
            assignmentId, PolicyScopeLevel.Department, "dept-1", TestAttendance.Today, null,
            Guid.NewGuid(), TestAttendance.NowUtc);

        result.IsSuccess.Should().BeTrue();
        policy.PolicyAssignments.Should().ContainSingle(a => a.Id == assignmentId && a.ScopeTargetId == "dept-1");
        policy.DomainEvents.OfType<AttendancePolicyAssigned>().Should().ContainSingle();
    }

    [Fact]
    public void Assign_Fails_WhenItOverlapsAnExistingAssignmentToTheSameScope()
    {
        var policy = NewPolicy();
        policy.Assign(
            new PolicyAssignmentId(Guid.NewGuid()), PolicyScopeLevel.Department, "dept-1", TestAttendance.Today,
            null, Guid.NewGuid(), TestAttendance.NowUtc);

        var result = policy.Assign(
            new PolicyAssignmentId(Guid.NewGuid()), PolicyScopeLevel.Department, "dept-1",
            TestAttendance.Today.AddDays(10), null, Guid.NewGuid(), TestAttendance.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AttendanceErrors.PolicyAssignmentOverlap);
    }

    [Fact]
    public void Assign_Succeeds_ForADifferentScopeTarget_EvenIfPeriodsOverlap()
    {
        var policy = NewPolicy();
        policy.Assign(
            new PolicyAssignmentId(Guid.NewGuid()), PolicyScopeLevel.Department, "dept-1", TestAttendance.Today,
            null, Guid.NewGuid(), TestAttendance.NowUtc);

        var result = policy.Assign(
            new PolicyAssignmentId(Guid.NewGuid()), PolicyScopeLevel.Department, "dept-2", TestAttendance.Today,
            null, Guid.NewGuid(), TestAttendance.NowUtc);

        result.IsSuccess.Should().BeTrue("AT-011 only constrains overlap against the same scope target");
    }

    [Fact]
    public void Unassign_EndDatesTheAssignment_RatherThanRemovingIt()
    {
        var policy = NewPolicy();
        var assignmentId = new PolicyAssignmentId(Guid.NewGuid());
        policy.Assign(assignmentId, PolicyScopeLevel.Department, "dept-1", TestAttendance.Today, null, Guid.NewGuid(), TestAttendance.NowUtc);
        var endDate = TestAttendance.Today.AddDays(5);

        var result = policy.Unassign(assignmentId, endDate, Guid.NewGuid(), TestAttendance.NowUtc);

        result.IsSuccess.Should().BeTrue();
        policy.PolicyAssignments.Should().ContainSingle(a => a.Id == assignmentId && a.EffectiveTo == endDate);
    }

    [Fact]
    public void Unassign_Fails_WhenTheAssignmentDoesNotExist()
    {
        var policy = NewPolicy();

        var result = policy.Unassign(new PolicyAssignmentId(Guid.NewGuid()), TestAttendance.Today, Guid.NewGuid(), TestAttendance.NowUtc);

        result.IsFailure.Should().BeTrue();
    }

    // ---- Retire --------------------------------------------------------

    [Fact]
    public void Retire_TransitionsToRetired()
    {
        var policy = NewPolicy();

        var result = policy.Retire(Guid.NewGuid(), "superseded", TestAttendance.NowUtc);

        result.IsSuccess.Should().BeTrue();
        policy.Status.Should().Be(AttendancePolicyStatus.Retired);
    }

    [Fact]
    public void Retire_IsIdempotent_WhenAlreadyRetired()
    {
        var policy = NewPolicy();
        policy.Retire(Guid.NewGuid(), "first", TestAttendance.NowUtc);
        policy.ClearDomainEvents();

        var result = policy.Retire(Guid.NewGuid(), "second", TestAttendance.NowUtc);

        result.IsSuccess.Should().BeTrue();
        policy.DomainEvents.Should().BeEmpty();
    }

    // ---- IsEffectiveOn ---------------------------------------------------

    [Fact]
    public void IsEffectiveOn_ReturnsTrue_WithinAnOpenEndedPeriod()
    {
        var policy = NewPolicy(TestAttendance.Today);

        policy.IsEffectiveOn(TestAttendance.Today.AddYears(1)).Should().BeTrue();
    }

    [Fact]
    public void IsEffectiveOn_ReturnsFalse_BeforeTheEffectiveStart()
    {
        var policy = NewPolicy(TestAttendance.Today.AddDays(10));

        policy.IsEffectiveOn(TestAttendance.Today).Should().BeFalse();
    }

    [Fact]
    public void IsEffectiveOn_ReturnsFalse_AfterTheVersionHasBeenSuperseded()
    {
        var policy = NewPolicy();
        policy.Publish(TestAttendance.Today, Guid.NewGuid(), TestAttendance.NowUtc);
        var newEffectiveFrom = TestAttendance.Today.AddDays(30);
        policy.Revise(new AttendancePolicyId(Guid.NewGuid()), TestAttendance.DefaultPolicy(), newEffectiveFrom, Guid.NewGuid(), TestAttendance.NowUtc);

        policy.IsEffectiveOn(newEffectiveFrom).Should().BeFalse("that date now belongs to the next version");
    }
}
