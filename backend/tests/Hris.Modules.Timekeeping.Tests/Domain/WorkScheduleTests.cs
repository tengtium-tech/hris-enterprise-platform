using FluentAssertions;
using Hris.Modules.Timekeeping.Domain;
using Xunit;

namespace Hris.Modules.Timekeeping.Tests.Domain;

public sealed class WorkScheduleTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private static DateOnly Today => TestTimekeeping.Today;

    [Fact]
    public void Create_Fails_WhenNameIsMissing()
    {
        var result = WorkSchedule.Create(
            new WorkScheduleId(Guid.NewGuid()), _tenantId, "  ", null, TestTimekeeping.MondayToFriday, null, null,
            Today, Guid.NewGuid(), TestTimekeeping.NowUtc);

        result.Error.Should().Be(TimekeepingErrors.WorkScheduleNameRequired);
    }

    /// <summary>TK-010.</summary>
    [Fact]
    public void Create_Fails_WhenNoWorkingDayIsNamed()
    {
        var result = WorkSchedule.Create(
            new WorkScheduleId(Guid.NewGuid()), _tenantId, "Empty", null, [], null, null, Today, Guid.NewGuid(),
            TestTimekeeping.NowUtc);

        result.Error.Should().Be(TimekeepingErrors.WorkingDayPatternRequiresAWorkingDay);
    }

    [Fact]
    public void Create_StartsAsADraftAtVersionOne_EstablishingItsOwnLineage()
    {
        var schedule = TestTimekeeping.Schedule(_tenantId);

        schedule.Status.Should().Be(WorkScheduleStatus.Draft);
        schedule.Version.Should().Be(1);
        schedule.LineageId.Should().Be(schedule.Id.Value);
    }

    [Fact]
    public void Publish_MakesItActive_AndRaisesTheEvent()
    {
        var schedule = TestTimekeeping.Schedule(_tenantId);

        var result = schedule.Publish(Guid.NewGuid(), TestTimekeeping.NowUtc);

        result.IsSuccess.Should().BeTrue();
        schedule.Status.Should().Be(WorkScheduleStatus.Active);
        schedule.DomainEvents.OfType<WorkSchedulePublished>().Should().ContainSingle();
    }

    /// <summary>TK-011: the check the nesting decision exists to make possible.</summary>
    [Fact]
    public void AssignTo_Fails_WhenAnotherAssignmentToTheSameTargetOverlaps()
    {
        var schedule = TestTimekeeping.ActiveSchedule(_tenantId);
        schedule.AssignTo(
            new ScheduleAssignmentId(Guid.NewGuid()), OrganizationalAssignmentLevel.Department, "dept-1", Today,
            Today.AddDays(30), Guid.NewGuid(), TestTimekeeping.NowUtc);

        var overlapping = schedule.AssignTo(
            new ScheduleAssignmentId(Guid.NewGuid()), OrganizationalAssignmentLevel.Department, "dept-1",
            Today.AddDays(10), Today.AddDays(40), Guid.NewGuid(), TestTimekeeping.NowUtc);

        overlapping.Error.Should().Be(TimekeepingErrors.ScheduleAssignmentOverlapsExisting);
    }

    [Fact]
    public void AssignTo_Succeeds_ForNonOverlappingPeriodsOnTheSameTarget()
    {
        var schedule = TestTimekeeping.ActiveSchedule(_tenantId);
        schedule.AssignTo(
            new ScheduleAssignmentId(Guid.NewGuid()), OrganizationalAssignmentLevel.Department, "dept-1", Today,
            Today.AddDays(30), Guid.NewGuid(), TestTimekeeping.NowUtc);

        var later = schedule.AssignTo(
            new ScheduleAssignmentId(Guid.NewGuid()), OrganizationalAssignmentLevel.Department, "dept-1",
            Today.AddDays(31), null, Guid.NewGuid(), TestTimekeeping.NowUtc);

        later.IsSuccess.Should().BeTrue();
        schedule.ScheduleAssignments.Should().HaveCount(2);
    }

    [Fact]
    public void AssignTo_Succeeds_ForTheSamePeriodOnADifferentTarget()
    {
        var schedule = TestTimekeeping.ActiveSchedule(_tenantId);
        schedule.AssignTo(
            new ScheduleAssignmentId(Guid.NewGuid()), OrganizationalAssignmentLevel.Department, "dept-1", Today, null,
            Guid.NewGuid(), TestTimekeeping.NowUtc);

        var other = schedule.AssignTo(
            new ScheduleAssignmentId(Guid.NewGuid()), OrganizationalAssignmentLevel.Department, "dept-2", Today, null,
            Guid.NewGuid(), TestTimekeeping.NowUtc);

        other.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void AssignTo_Fails_WhenTheTargetIsMissing()
    {
        var schedule = TestTimekeeping.ActiveSchedule(_tenantId);

        var result = schedule.AssignTo(
            new ScheduleAssignmentId(Guid.NewGuid()), OrganizationalAssignmentLevel.Department, " ", Today, null,
            Guid.NewGuid(), TestTimekeeping.NowUtc);

        result.Error.Should().Be(TimekeepingErrors.AssignmentTargetRequired);
    }

    /// <summary>TK-012: end-dated, never deleted.</summary>
    [Fact]
    public void Unassign_EndDatesTheAssignment_RatherThanRemovingIt()
    {
        var schedule = TestTimekeeping.ActiveSchedule(_tenantId);
        var assignmentId = schedule.AssignTo(
            new ScheduleAssignmentId(Guid.NewGuid()), OrganizationalAssignmentLevel.Department, "dept-1", Today, null,
            Guid.NewGuid(), TestTimekeeping.NowUtc).Value;

        var result = schedule.Unassign(assignmentId, Today.AddDays(5), Guid.NewGuid(), TestTimekeeping.NowUtc);

        result.IsSuccess.Should().BeTrue();
        schedule.ScheduleAssignments.Should().ContainSingle("the record is evidence of a past period, not deleted");
        schedule.ScheduleAssignments[0].EffectiveTo.Should().Be(Today.AddDays(5));
        schedule.DomainEvents.OfType<WorkScheduleUnassignedFromOrganizationalUnit>().Should().ContainSingle();
    }

    [Fact]
    public void Unassign_Fails_WhenTheAssignmentIsNotPartOfThisSchedule()
    {
        var schedule = TestTimekeeping.ActiveSchedule(_tenantId);

        var result = schedule.Unassign(
            new ScheduleAssignmentId(Guid.NewGuid()), Today, Guid.NewGuid(), TestTimekeeping.NowUtc);

        result.Error.Should().Be(TimekeepingErrors.ScheduleAssignmentNotFound);
    }

    [Fact]
    public void AssignmentOn_ReturnsTheAssignmentGoverningThatDate()
    {
        var schedule = TestTimekeeping.ActiveSchedule(_tenantId);
        schedule.AssignTo(
            new ScheduleAssignmentId(Guid.NewGuid()), OrganizationalAssignmentLevel.Department, "dept-1", Today,
            Today.AddDays(10), Guid.NewGuid(), TestTimekeeping.NowUtc);

        schedule.AssignmentOn(OrganizationalAssignmentLevel.Department, "dept-1", Today.AddDays(5)).Should().NotBeNull();
        schedule.AssignmentOn(OrganizationalAssignmentLevel.Department, "dept-1", Today.AddDays(20)).Should().BeNull();
    }

    [Fact]
    public void Retire_IsIdempotent_AndRaisesOneEvent()
    {
        var schedule = TestTimekeeping.ActiveSchedule(_tenantId);
        schedule.Retire(Guid.NewGuid(), Today, TestTimekeeping.NowUtc);

        var second = schedule.Retire(Guid.NewGuid(), Today, TestTimekeeping.NowUtc);

        second.IsSuccess.Should().BeTrue();
        schedule.DomainEvents.OfType<WorkScheduleRetired>().Should().ContainSingle();
    }

    [Fact]
    public void Retire_Fails_OnADraft()
    {
        var draft = TestTimekeeping.Schedule(_tenantId);

        draft.Retire(Guid.NewGuid(), Today, TestTimekeeping.NowUtc).Error
            .Should().Be(TimekeepingErrors.VersionNotActive);
    }
}
