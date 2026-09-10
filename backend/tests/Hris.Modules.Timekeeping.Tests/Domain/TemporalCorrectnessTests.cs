using FluentAssertions;
using Hris.Modules.Timekeeping.Domain;
using Xunit;

namespace Hris.Modules.Timekeeping.Tests.Domain;

/// <summary>
/// TK-001 through TK-003, this module's core. business-rules.md calls them its
/// security core in the sense the module README uses the term: not permissions, but
/// properties nothing may violate regardless of who is acting.
///
/// These tests assert the shape of the failure rather than merely that operations
/// succeed, because a violation here does not produce an error message. It produces
/// payroll two phases downstream that is internally consistent, plausible, and
/// wrong. So each test below checks that the *historical* answer is unchanged, not
/// just that a revision was accepted.
/// </summary>
public sealed class TemporalCorrectnessTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    /// <summary>
    /// TK-001. The superseded version's own fields must read exactly as they did
    /// before supersession — a payroll run that read version 1 six months ago must
    /// still read the same thing today.
    /// </summary>
    [Fact]
    public void TK001_SupersedingASchedule_LeavesThePreviousVersionUnchanged()
    {
        var original = TestTimekeeping.ActiveSchedule(_tenantId, TestTimekeeping.Today);
        var originalWorkingDays = original.WorkingDayPattern.WorkingDays.ToList();
        var originalHours = original.StandardHours;
        var originalVersion = original.Version;

        var next = original.Supersede(
            new WorkScheduleId(Guid.NewGuid()), [DayOfWeek.Monday], TestTimekeeping.Window(8, 17), null,
            TestTimekeeping.Today.AddDays(30), Guid.NewGuid(), TestTimekeeping.NowUtc);

        next.IsSuccess.Should().BeTrue();

        original.WorkingDayPattern.WorkingDays.Should().BeEquivalentTo(originalWorkingDays);
        original.StandardHours.Should().Be(originalHours);
        original.Version.Should().Be(originalVersion);
        original.Status.Should().Be(WorkScheduleStatus.Superseded);
    }

    [Fact]
    public void TK001_SupersedingAShift_LeavesThePreviousVersionUnchanged()
    {
        var original = TestTimekeeping.ActiveShift(_tenantId);
        var originalTiming = original.Timing;
        var originalOvertime = original.OvertimeEligible;

        original.Supersede(
            new WorkShiftId(Guid.NewGuid()), "Revised", TestTimekeeping.FixedTiming(6, 14), false, null, null, null,
            !originalOvertime, PremiumEligibilityFlags.None, TestTimekeeping.Today.AddDays(30), Guid.NewGuid(),
            TestTimekeeping.NowUtc);

        original.Timing.Should().Be(originalTiming);
        original.OvertimeEligible.Should().Be(originalOvertime);
        original.Name.Should().Be("Day Shift", "the superseded version keeps its own name");
    }

    /// <summary>
    /// TK-001 again, from the other direction: the aggregate refuses to be modified
    /// once superseded, so a caller cannot reach past the versioning mechanism.
    /// </summary>
    [Fact]
    public void TK001_ASupersededScheduleRefusesFurtherModification()
    {
        var original = TestTimekeeping.ActiveSchedule(_tenantId);
        original.Supersede(
            new WorkScheduleId(Guid.NewGuid()), TestTimekeeping.MondayToFriday, null, null,
            TestTimekeeping.Today.AddDays(30), Guid.NewGuid(), TestTimekeeping.NowUtc);

        var assign = original.AssignTo(
            new ScheduleAssignmentId(Guid.NewGuid()), OrganizationalAssignmentLevel.Department, "dept-1",
            TestTimekeeping.Today, null, Guid.NewGuid(), TestTimekeeping.NowUtc);

        assign.Error.Should().Be(TimekeepingErrors.SupersededVersionCannotBeModified);
    }

    /// <summary>
    /// TK-002, and the reason CTR-DAT-005 names this module. The old version stays
    /// effective for dates before the new one takes over, and the new version is not
    /// effective on those dates at all — which is what makes a query about last month
    /// return last month's rule.
    /// </summary>
    [Fact]
    public void TK002_EachVersionIsEffectiveOnlyForItsOwnDateRange()
    {
        var original = TestTimekeeping.ActiveSchedule(_tenantId, TestTimekeeping.Today);
        var changeover = TestTimekeeping.Today.AddDays(30);

        var next = original.Supersede(
            new WorkScheduleId(Guid.NewGuid()), [DayOfWeek.Monday], null, null, changeover, Guid.NewGuid(),
            TestTimekeeping.NowUtc).Value;

        var dayBefore = changeover.AddDays(-1);

        original.IsEffectiveOn(dayBefore).Should().BeTrue();
        original.IsEffectiveOn(changeover).Should().BeFalse();

        next.IsEffectiveOn(dayBefore).Should().BeFalse("a revision must not reach back before its own effective date");
        next.IsEffectiveOn(changeover).Should().BeTrue();
    }

    /// <summary>
    /// A new version taking effect on or before the version it replaces would leave
    /// two versions applicable on the same date, making TK-002's selection ambiguous.
    /// </summary>
    [Fact]
    public void TK002_ARevisionMustTakeEffectAfterTheVersionItReplaces()
    {
        var original = TestTimekeeping.ActiveSchedule(_tenantId, TestTimekeeping.Today);

        var sameDay = original.Supersede(
            new WorkScheduleId(Guid.NewGuid()), TestTimekeeping.MondayToFriday, null, null, TestTimekeeping.Today,
            Guid.NewGuid(), TestTimekeeping.NowUtc);

        var earlier = original.Supersede(
            new WorkScheduleId(Guid.NewGuid()), TestTimekeeping.MondayToFriday, null, null,
            TestTimekeeping.Today.AddDays(-1), Guid.NewGuid(), TestTimekeeping.NowUtc);

        sameDay.Error.Should().Be(TimekeepingErrors.EffectiveFromNotAfterCurrentVersion);
        earlier.Error.Should().Be(TimekeepingErrors.EffectiveFromNotAfterCurrentVersion);
    }

    [Fact]
    public void TK002_AVersionChainSharesOneLineage()
    {
        var v1 = TestTimekeeping.ActiveSchedule(_tenantId);
        var v2 = v1.Supersede(
            new WorkScheduleId(Guid.NewGuid()), TestTimekeeping.MondayToFriday, null, null,
            TestTimekeeping.Today.AddDays(30), Guid.NewGuid(), TestTimekeeping.NowUtc).Value;

        v2.LineageId.Should().Be(v1.LineageId);
        v2.Version.Should().Be(v1.Version + 1);
        v2.Id.Should().NotBe(v1.Id, "a version is a new aggregate instance, not an edit");
    }

    /// <summary>
    /// TK-002 applied to holiday calendars, including that a revision carries the
    /// existing entries forward so revising does not mean re-entering a year of
    /// holidays.
    /// </summary>
    [Fact]
    public void TK002_RevisingAHolidayCalendarCopiesEntriesAndLeavesTheOriginalIntact()
    {
        var holidayDate = TestTimekeeping.Today.AddDays(5);
        var v1 = TestTimekeeping.PublishedCountryCalendar(holidayDate);

        var v2 = v1.Supersede(
            new HolidayCalendarId(Guid.NewGuid()), TestTimekeeping.Today.AddDays(60), Guid.NewGuid(),
            TestTimekeeping.NowUtc).Value;

        v2.Holidays.Should().ContainSingle();
        v2.Holidays[0].Date.Should().Be(holidayDate);
        v2.Status.Should().Be(HolidayCalendarStatus.Draft, "a copied version is authored before it is published");

        v1.Holidays.Should().ContainSingle("the superseded version keeps its own entries");
        v1.Status.Should().Be(HolidayCalendarStatus.Superseded);
    }

    /// <summary>
    /// TK-003's checkable half. This module discharges the obligation by never
    /// mutating a superseded version and by resolving on the evaluated date; the
    /// finalized-result half cannot be asserted until attendance and payroll exist,
    /// which is recorded as a documented gap rather than silently assumed.
    /// </summary>
    [Fact]
    public void TK003_PublishingARevisionDoesNotAlterAnyPriorVersionsResolvedAnswer()
    {
        var v1 = TestTimekeeping.ActiveSchedule(_tenantId, TestTimekeeping.Today);
        var pastDate = TestTimekeeping.Today.AddDays(3);
        var beforeRevision = v1.WorkingDayPattern.IsWorkingDay(pastDate);

        v1.Supersede(
            new WorkScheduleId(Guid.NewGuid()), [DayOfWeek.Sunday], null, null, TestTimekeeping.Today.AddDays(30),
            Guid.NewGuid(), TestTimekeeping.NowUtc);

        v1.WorkingDayPattern.IsWorkingDay(pastDate).Should().Be(beforeRevision);
    }
}
