using FluentAssertions;
using Hris.Foundation.Scheduling.Domain;
using Xunit;

namespace Hris.Foundation.Scheduling.Tests.Domain;

public sealed class ScheduleTests
{
    [Fact]
    public void Create_Succeeds_InDraft_AndRaisesScheduleCreated()
    {
        var expression = TestData.NewExpression();
        var timeZone = TestData.NewTimeZone();

        var result = Schedule.Create(
            TestData.TenantId, ScheduleType.CronBased, expression, timeZone, "PayrollProcessing", "run-1", HolidayBehavior.SkipHolidays, "PayrollCalendar", TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        result.Value.TenantId.Should().Be(TestData.TenantId);
        result.Value.ScheduleType.Should().Be(ScheduleType.CronBased);
        result.Value.Expression.Should().Be(expression);
        result.Value.TimeZone.Should().Be(timeZone);
        result.Value.TaskType.Should().Be("PayrollProcessing");
        result.Value.TaskReferenceId.Should().Be("run-1");
        result.Value.HolidayBehavior.Should().Be(HolidayBehavior.SkipHolidays);
        result.Value.CalendarReference.Should().Be("PayrollCalendar");
        result.Value.Status.Should().Be(ScheduleStatus.Draft);
        result.Value.DomainEvents.OfType<ScheduleCreated>().Should().ContainSingle();
    }

    [Fact]
    public void Create_Throws_WhenTenantIdIsEmpty()
    {
        var act = () => Schedule.Create(
            Guid.Empty, ScheduleType.CronBased, TestData.NewExpression(), TestData.NewTimeZone(), "PayrollProcessing", null, HolidayBehavior.ExecuteNormally, null, TestData.NowUtc);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Fails_WhenTaskTypeIsMissing(string? taskType)
    {
        var result = Schedule.Create(
            TestData.TenantId, ScheduleType.CronBased, TestData.NewExpression(), TestData.NewTimeZone(), taskType, null, HolidayBehavior.ExecuteNormally, null, TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SchedulingErrors.TaskTypeRequired);
    }

    [Fact]
    public void Update_Succeeds_WhileDraft_AndRaisesScheduleUpdated()
    {
        var schedule = TestData.DraftSchedule();
        var newExpression = TestData.NewExpression("0 6 * * *");

        var result = schedule.Update(newExpression, TestData.NewTimeZone(), "AttendanceReconciliation", "run-2", HolidayBehavior.PauseDuringShutdown, "CompanyCalendar", TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        schedule.Expression.Should().Be(newExpression);
        schedule.TaskType.Should().Be("AttendanceReconciliation");
        schedule.HolidayBehavior.Should().Be(HolidayBehavior.PauseDuringShutdown);
        schedule.DomainEvents.OfType<ScheduleUpdated>().Should().ContainSingle();
    }

    [Fact]
    public void Update_Fails_AfterValidated()
    {
        var schedule = TestData.ValidatedSchedule();

        var result = schedule.Update(TestData.NewExpression(), TestData.NewTimeZone(), "PayrollProcessing", null, HolidayBehavior.ExecuteNormally, null, TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SchedulingErrors.InvalidScheduleLifecycleTransition);
    }

    [Fact]
    public void Validate_Succeeds_FromDraft_AndRaisesNoEvent()
    {
        var schedule = TestData.DraftSchedule();

        var result = schedule.Validate();

        result.IsSuccess.Should().BeTrue();
        schedule.Status.Should().Be(ScheduleStatus.Validated);
        schedule.DomainEvents.OfType<ScheduleCreated>().Should().ContainSingle();
        schedule.DomainEvents.Should().HaveCount(1, "scheduling-framework.md's own Domain Events list names no ScheduleValidated event");
    }

    [Fact]
    public void Validate_Fails_WhenNotDraft()
    {
        var schedule = TestData.ValidatedSchedule();

        var result = schedule.Validate();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SchedulingErrors.InvalidScheduleLifecycleTransition);
    }

    [Fact]
    public void Approve_Succeeds_FromValidated_AndRaisesNoEvent()
    {
        var schedule = TestData.ValidatedSchedule();

        var result = schedule.Approve();

        result.IsSuccess.Should().BeTrue();
        schedule.Status.Should().Be(ScheduleStatus.Approved);
    }

    [Fact]
    public void Approve_Fails_WhenNotValidated()
    {
        var schedule = TestData.DraftSchedule();

        var result = schedule.Approve();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SchedulingErrors.InvalidScheduleLifecycleTransition);
    }

    [Fact]
    public void Activate_Succeeds_FromApproved_AndRaisesScheduleActivated()
    {
        var schedule = TestData.ApprovedSchedule();

        var result = schedule.Activate(TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        schedule.Status.Should().Be(ScheduleStatus.Active);
        schedule.LastTransitionAtUtc.Should().Be(TestData.NowUtc);
        schedule.DomainEvents.OfType<ScheduleActivated>().Should().ContainSingle();
    }

    [Fact]
    public void Activate_Fails_WhenNotApproved()
    {
        var schedule = TestData.DraftSchedule();

        var result = schedule.Activate(TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SchedulingErrors.InvalidScheduleLifecycleTransition);
    }

    [Fact]
    public void Pause_Succeeds_FromActive_AndRaisesSchedulePaused()
    {
        var schedule = TestData.ActiveSchedule();

        var result = schedule.Pause(TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        schedule.Status.Should().Be(ScheduleStatus.Paused);
        schedule.DomainEvents.OfType<SchedulePaused>().Should().ContainSingle();
    }

    [Fact]
    public void Pause_Succeeds_FromResumed()
    {
        var schedule = TestData.ActiveSchedule();
        schedule.Pause(TestData.NowUtc);
        schedule.Resume(TestData.NowUtc);

        var result = schedule.Pause(TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        schedule.Status.Should().Be(ScheduleStatus.Paused);
    }

    [Fact]
    public void Pause_Fails_WhenDraft()
    {
        var schedule = TestData.DraftSchedule();

        var result = schedule.Pause(TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SchedulingErrors.InvalidScheduleLifecycleTransition);
    }

    [Fact]
    public void Resume_Succeeds_FromPaused_AndRaisesScheduleResumed()
    {
        var schedule = TestData.ActiveSchedule();
        schedule.Pause(TestData.NowUtc);

        var result = schedule.Resume(TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        schedule.Status.Should().Be(ScheduleStatus.Resumed);
        schedule.DomainEvents.OfType<ScheduleResumed>().Should().ContainSingle();
    }

    [Fact]
    public void Resume_Fails_WhenNotPaused()
    {
        var schedule = TestData.ActiveSchedule();

        var result = schedule.Resume(TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SchedulingErrors.InvalidScheduleLifecycleTransition);
    }

    [Theory]
    [InlineData(ScheduleStatus.Draft)]
    [InlineData(ScheduleStatus.Active)]
    [InlineData(ScheduleStatus.Paused)]
    [InlineData(ScheduleStatus.Resumed)]
    public void Retire_Succeeds_FromAnyNonRetiredState_AndRaisesScheduleRetired(ScheduleStatus status)
    {
        var schedule = status switch
        {
            ScheduleStatus.Draft => TestData.DraftSchedule(),
            ScheduleStatus.Active => TestData.ActiveSchedule(),
            ScheduleStatus.Paused => Paused(),
            ScheduleStatus.Resumed => Resumed(),
            _ => throw new ArgumentOutOfRangeException(nameof(status)),
        };

        var result = schedule.Retire(TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        schedule.Status.Should().Be(ScheduleStatus.Retired);
        schedule.DomainEvents.OfType<ScheduleRetired>().Should().ContainSingle();

        static Schedule Paused()
        {
            var s = TestData.ActiveSchedule();
            s.Pause(TestData.NowUtc);
            return s;
        }

        static Schedule Resumed()
        {
            var s = TestData.ActiveSchedule();
            s.Pause(TestData.NowUtc);
            s.Resume(TestData.NowUtc);
            return s;
        }
    }

    [Fact]
    public void Retire_Fails_WhenAlreadyRetired()
    {
        var schedule = TestData.ActiveSchedule();
        schedule.Retire(TestData.NowUtc);

        var result = schedule.Retire(TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SchedulingErrors.InvalidScheduleLifecycleTransition);
    }
}
