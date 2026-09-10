using FluentAssertions;
using Hris.Modules.Timekeeping.Application.Commands;
using Hris.Modules.Timekeeping.Application.Mapping;
using Hris.Modules.Timekeeping.Application.Validators;
using Hris.Modules.Timekeeping.Domain;
using Xunit;

namespace Hris.Modules.Timekeeping.Tests.Application;

public sealed class TimekeepingMapperTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private static DateOnly Today => TestTimekeeping.Today;

    [Fact]
    public void ToDto_ProjectsAScheduleIncludingDerivedRestDays()
    {
        var schedule = TestTimekeeping.ActiveSchedule(_tenantId);
        schedule.AssignTo(
            new ScheduleAssignmentId(Guid.NewGuid()), OrganizationalAssignmentLevel.Department, "dept-1", Today, null,
            Guid.NewGuid(), TestTimekeeping.NowUtc);

        var dto = TimekeepingMapper.ToDto(schedule);

        dto.WorkingDays.Should().HaveCount(5);
        dto.RestDays.Should().BeEquivalentTo(["Saturday", "Sunday"]);
        dto.StandardHoursStart.Should().Be(new TimeOnly(9, 0));
        dto.ScheduleAssignments.Should().ContainSingle();
        dto.Status.Should().Be("Active");
    }

    [Fact]
    public void ToDto_ProjectsAFixedShiftWithoutFlexibleFields()
    {
        var dto = TimekeepingMapper.ToDto(TestTimekeeping.ActiveShift(_tenantId));

        dto.Timing.Kind.Should().Be("Fixed");
        dto.Timing.FixedStart.Should().Be(new TimeOnly(9, 0));
        dto.Timing.CoreStart.Should().BeNull();
        dto.AnchorRule.Should().BeNull();
        dto.PremiumEligibility.NightDifferentialEligible.Should().BeFalse();
    }

    [Fact]
    public void ToDto_ProjectsAFlexibleShiftAndAnAnchorRule()
    {
        var timing = ShiftTiming.Flexible(
            new TimeOnly(7, 0), new TimeOnly(10, 0), TestTimekeeping.Window(10, 15), TimeSpan.FromHours(8)).Value;

        var shift = WorkShift.Create(
            new WorkShiftId(Guid.NewGuid()), _tenantId, "FLEX", "Flex", timing, true,
            WorkDateAnchorRule.AnchoredToShiftStart, null, null, true,
            PremiumEligibilityFlags.Create(true, true, true), Today, false, Guid.NewGuid(),
            TestTimekeeping.NowUtc).Value;

        var dto = TimekeepingMapper.ToDto(shift);

        dto.Timing.Kind.Should().Be("Flexible");
        dto.Timing.RequiredHours.Should().Be(TimeSpan.FromHours(8));
        dto.AnchorRule!.AnchorPoint.Should().Be("ShiftStart");
        dto.PremiumEligibility.HazardEligible.Should().BeTrue();
    }

    [Fact]
    public void ToDto_ProjectsSplitPeriodsAndBreakRules()
    {
        IReadOnlyList<ShiftPeriod> periods =
        [
            new(1, new TimeOnly(8, 0), new TimeOnly(12, 0)),
            new(2, new TimeOnly(13, 0), new TimeOnly(17, 0)),
        ];
        IReadOnlyList<BreakRule> breaks =
        [
            new(BreakKind.Meal, TimeSpan.FromMinutes(60), new TimeWindowSnapshot(new TimeOnly(12, 0), new TimeOnly(13, 0)), false, true),
        ];

        var shift = WorkShift.Create(
            new WorkShiftId(Guid.NewGuid()), _tenantId, "SPLIT", "Split", TestTimekeeping.FixedTiming(), false, null,
            periods, breaks, true, PremiumEligibilityFlags.None, Today, false, Guid.NewGuid(),
            TestTimekeeping.NowUtc).Value;

        var dto = TimekeepingMapper.ToDto(shift);

        dto.SplitPeriods.Should().HaveCount(2);
        dto.BreakRules.Should().ContainSingle();
        dto.BreakRules[0].WindowStart.Should().Be(new TimeOnly(12, 0));
        dto.BreakRules[0].Kind.Should().Be("Meal");
    }

    [Fact]
    public void ToDto_ProjectsAnAssignmentIncludingAPendingSwapWithBothIdentities()
    {
        var primary = TestTimekeeping.Assignment(_tenantId, "emp-1");
        var secondary = TestTimekeeping.Assignment(_tenantId, "emp-2");
        primary.ProposeSwap(secondary.Id, "emp-1", "emp-2", Today, Guid.NewGuid());
        primary.ConsentToSwap("emp-1");

        var dto = TimekeepingMapper.ToDto(primary);

        dto.PendingSwap.Should().NotBeNull();
        dto.PendingSwap!.PrimaryEmployeeId.Should().Be("emp-1");
        dto.PendingSwap.CounterpartEmployeeId.Should().Be("emp-2");
        dto.PendingSwap.PrimaryConsented.Should().BeTrue();
        dto.PendingSwap.IsReadyToActivate.Should().BeFalse();
        dto.Status.Should().Be("Scheduled");
    }

    [Fact]
    public void ToDto_LeavesPendingSwapNull_WhenNoneIsProposed()
    {
        TimekeepingMapper.ToDto(TestTimekeeping.Assignment(_tenantId, "emp-1")).PendingSwap.Should().BeNull();
    }

    [Fact]
    public void ToDto_ProjectsACalendarAndItsHolidays()
    {
        var country = TestTimekeeping.PublishedCountryCalendar(Today);

        var dto = TimekeepingMapper.ToDto(country);

        dto.TenantId.Should().BeNull();
        dto.Level.Should().Be("Country");
        dto.CountryCode.Should().Be("PH");
        dto.Holidays.Should().ContainSingle();
        dto.Holidays[0].Type.Should().Be("RegularHoliday");
        dto.Holidays[0].WorkRule.Should().Be("NoWorkExpected");
    }

    [Fact]
    public void ToDto_ProjectsResolutionOutcomesDistinctly()
    {
        var assignment = TestTimekeeping.Assignment(_tenantId, "emp-1");

        TimekeepingMapper.ToDto(ShiftResolution.Resolved(assignment)).IsUnresolved.Should().BeFalse();
        TimekeepingMapper.ToDto(ShiftResolution.Unresolved).IsUnresolved.Should().BeTrue();
        TimekeepingMapper.ToDto(ShiftResolution.Ambiguous).IsAmbiguous.Should().BeTrue();
        TimekeepingMapper.ToDto((HolidayResolution?)null).IsHoliday.Should().BeFalse();
    }

    [Fact]
    public void ToDto_Throws_OnNullInput()
    {
        ((Action)(() => TimekeepingMapper.ToDto((WorkSchedule)null!))).Should().Throw<ArgumentNullException>();
        ((Action)(() => TimekeepingMapper.ToDto((WorkShift)null!))).Should().Throw<ArgumentNullException>();
        ((Action)(() => TimekeepingMapper.ToDto((ShiftAssignment)null!))).Should().Throw<ArgumentNullException>();
        ((Action)(() => TimekeepingMapper.ToDto((HolidayCalendar)null!))).Should().Throw<ArgumentNullException>();
        ((Action)(() => TimekeepingMapper.ToDto((Holiday)null!))).Should().Throw<ArgumentNullException>();
        ((Action)(() => TimekeepingMapper.ToDto((ShiftTiming)null!))).Should().Throw<ArgumentNullException>();
        ((Action)(() => TimekeepingMapper.ToDto((BreakRule)null!))).Should().Throw<ArgumentNullException>();
        ((Action)(() => TimekeepingMapper.ToDto((PendingShiftSwap)null!))).Should().Throw<ArgumentNullException>();
        ((Action)(() => TimekeepingMapper.ToDto((ScheduleAssignment)null!))).Should().Throw<ArgumentNullException>();
        ((Action)(() => TimekeepingMapper.ToDto((ShiftResolution)null!))).Should().Throw<ArgumentNullException>();
    }
}

public sealed class TimekeepingValidatorTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _actor = Guid.NewGuid();
    private static DateOnly Today => TestTimekeeping.Today;

    private static ShiftTimingInput FixedInput() =>
        new(ShiftTimingKind.Fixed, new TimeOnly(9, 0), new TimeOnly(18, 0), null, null, null, null, null);

    [Fact]
    public void DefineWorkSchedule_RequiresNameAndWorkingDays()
    {
        var validator = new DefineWorkScheduleCommandValidator();

        validator.Validate(new DefineWorkScheduleCommand(
            _tenantId, "Standard", null, TestTimekeeping.MondayToFriday, null, null, null, Today, _actor))
            .IsValid.Should().BeTrue();

        validator.Validate(new DefineWorkScheduleCommand(
            _tenantId, "", null, TestTimekeeping.MondayToFriday, null, null, null, Today, _actor))
            .IsValid.Should().BeFalse();

        validator.Validate(new DefineWorkScheduleCommand(
            _tenantId, "Standard", null, [], null, null, null, Today, _actor)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void AssignWorkSchedule_RequiresAnOrderedPeriodAndTarget()
    {
        var validator = new AssignWorkScheduleCommandValidator();

        validator.Validate(new AssignWorkScheduleCommand(
            _tenantId, Guid.NewGuid(), OrganizationalAssignmentLevel.Department, "dept-1", Today, Today.AddDays(5),
            _actor)).IsValid.Should().BeTrue();

        validator.Validate(new AssignWorkScheduleCommand(
            _tenantId, Guid.NewGuid(), OrganizationalAssignmentLevel.Department, "dept-1", Today, Today.AddDays(-1),
            _actor)).IsValid.Should().BeFalse();

        validator.Validate(new AssignWorkScheduleCommand(
            _tenantId, Guid.NewGuid(), OrganizationalAssignmentLevel.Department, " ", Today, null, _actor))
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public void DefineWorkShift_RequiresCodeNameAndTiming()
    {
        var validator = new DefineWorkShiftCommandValidator();

        validator.Validate(new DefineWorkShiftCommand(
            _tenantId, "DAY", "Day", FixedInput(), false, null, null, null, null, true, false, false, false, Today,
            _actor)).IsValid.Should().BeTrue();

        validator.Validate(new DefineWorkShiftCommand(
            _tenantId, "", "Day", FixedInput(), false, null, null, null, null, true, false, false, false, Today,
            _actor)).IsValid.Should().BeFalse();

        validator.Validate(new DefineWorkShiftCommand(
            _tenantId, "DAY", "Day", null, false, null, null, null, null, true, false, false, false, Today, _actor))
            .IsValid.Should().BeFalse();
    }

    /// <summary>TK-032 at the shape level, so a caller learns before dispatch.</summary>
    [Fact]
    public void CreateShiftAssignment_RequiresAnEndDateWhenTemporary()
    {
        var validator = new CreateShiftAssignmentCommandValidator();

        validator.Validate(new CreateShiftAssignmentCommand(
            _tenantId, AssignmentTargetType.Employee, "emp-1", OrganizationalAssignmentLevel.IndividualEmployee,
            Guid.NewGuid(), Today, null, true, null, _actor)).IsValid.Should().BeFalse();

        validator.Validate(new CreateShiftAssignmentCommand(
            _tenantId, AssignmentTargetType.Employee, "emp-1", OrganizationalAssignmentLevel.IndividualEmployee,
            Guid.NewGuid(), Today, Today.AddDays(14), true, null, _actor)).IsValid.Should().BeTrue();
    }

    [Fact]
    public void ProposeShiftSwap_RejectsSwappingAnAssignmentWithItself()
    {
        var validator = new ProposeShiftSwapCommandValidator();
        var id = Guid.NewGuid();

        validator.Validate(new ProposeShiftSwapCommand(_tenantId, id, id, Today, _actor)).IsValid.Should().BeFalse();
        validator.Validate(new ProposeShiftSwapCommand(_tenantId, id, Guid.NewGuid(), Today, _actor))
            .IsValid.Should().BeTrue();
    }

    [Fact]
    public void SwapAndCalendarValidators_CheckTheirRequiredFields()
    {
        new ConsentToShiftSwapCommandValidator()
            .Validate(new ConsentToShiftSwapCommand(_tenantId, Guid.NewGuid(), "emp-1")).IsValid.Should().BeTrue();
        new ConsentToShiftSwapCommandValidator()
            .Validate(new ConsentToShiftSwapCommand(_tenantId, Guid.NewGuid(), " ")).IsValid.Should().BeFalse();

        new OverrideShiftSwapCommandValidator()
            .Validate(new OverrideShiftSwapCommand(_tenantId, Guid.NewGuid(), _actor, "No-show", true))
            .IsValid.Should().BeTrue();
        new OverrideShiftSwapCommandValidator()
            .Validate(new OverrideShiftSwapCommand(_tenantId, Guid.NewGuid(), _actor, "", true))
            .IsValid.Should().BeFalse();

        new ActivateShiftSwapCommandValidator()
            .Validate(new ActivateShiftSwapCommand(_tenantId, Guid.NewGuid())).IsValid.Should().BeTrue();

        new CancelShiftAssignmentCommandValidator()
            .Validate(new CancelShiftAssignmentCommand(_tenantId, Guid.NewGuid(), "Reason", Today, _actor))
            .IsValid.Should().BeTrue();
        new CancelShiftAssignmentCommandValidator()
            .Validate(new CancelShiftAssignmentCommand(_tenantId, Guid.NewGuid(), "", Today, _actor))
            .IsValid.Should().BeFalse();

        new DefineHolidayCalendarCommandValidator()
            .Validate(new DefineHolidayCalendarCommand(
                _tenantId, "PH", HolidayCalendarLevel.Country, "PH", "PH", null, Today, true, _actor))
            .IsValid.Should().BeTrue();
        new DefineHolidayCalendarCommandValidator()
            .Validate(new DefineHolidayCalendarCommand(
                _tenantId, "PH", HolidayCalendarLevel.Country, "PH", "", null, Today, true, _actor))
            .IsValid.Should().BeFalse();

        new AddHolidayCommandValidator()
            .Validate(new AddHolidayCommand(
                _tenantId, Guid.NewGuid(), Today, "Day", HolidayType.CompanyHoliday, HolidayWorkRule.NoWorkExpected,
                false, _actor)).IsValid.Should().BeTrue();

        new RemoveHolidayCommandValidator()
            .Validate(new RemoveHolidayCommand(_tenantId, Guid.NewGuid(), Guid.NewGuid(), false, _actor))
            .IsValid.Should().BeTrue();

        new PublishHolidayCalendarCommandValidator()
            .Validate(new PublishHolidayCalendarCommand(_tenantId, Guid.NewGuid(), _actor)).IsValid.Should().BeTrue();

        new ReviseHolidayCalendarCommandValidator()
            .Validate(new ReviseHolidayCalendarCommand(_tenantId, Guid.NewGuid(), Today, _actor))
            .IsValid.Should().BeTrue();

        new ChangeHolidayCalendarLayerCommandValidator()
            .Validate(new ChangeHolidayCalendarLayerCommand(_tenantId, Guid.NewGuid(), Guid.NewGuid(), _actor))
            .IsValid.Should().BeTrue();
    }

    [Fact]
    public void ScheduleAndShiftLifecycleValidators_CheckIdentifiers()
    {
        new PublishWorkScheduleCommandValidator()
            .Validate(new PublishWorkScheduleCommand(_tenantId, Guid.NewGuid(), _actor)).IsValid.Should().BeTrue();
        new PublishWorkScheduleCommandValidator()
            .Validate(new PublishWorkScheduleCommand(Guid.Empty, Guid.NewGuid(), _actor)).IsValid.Should().BeFalse();

        new ReviseWorkScheduleCommandValidator()
            .Validate(new ReviseWorkScheduleCommand(
                _tenantId, Guid.NewGuid(), TestTimekeeping.MondayToFriday, null, null, null, Today, _actor))
            .IsValid.Should().BeTrue();

        new RetireWorkScheduleCommandValidator()
            .Validate(new RetireWorkScheduleCommand(_tenantId, Guid.NewGuid(), Today, _actor))
            .IsValid.Should().BeTrue();

        new UnassignWorkScheduleCommandValidator()
            .Validate(new UnassignWorkScheduleCommand(_tenantId, Guid.NewGuid(), Guid.NewGuid(), Today, _actor))
            .IsValid.Should().BeTrue();

        new PublishWorkShiftCommandValidator()
            .Validate(new PublishWorkShiftCommand(_tenantId, Guid.NewGuid(), _actor)).IsValid.Should().BeTrue();

        new ReviseWorkShiftCommandValidator()
            .Validate(new ReviseWorkShiftCommand(
                _tenantId, Guid.NewGuid(), "Revised", FixedInput(), false, null, null, null, null, true, false, false,
                false, Today, _actor)).IsValid.Should().BeTrue();

        new RetireWorkShiftCommandValidator()
            .Validate(new RetireWorkShiftCommand(_tenantId, Guid.NewGuid(), Today, _actor)).IsValid.Should().BeTrue();
    }
}

public sealed class WorkShiftInputConversionTests
{
    [Fact]
    public void ShiftTimingInput_Fixed_FailsWithoutAWindow()
    {
        var input = new ShiftTimingInput(ShiftTimingKind.Fixed, null, null, null, null, null, null, null);

        input.ToTiming().Error.Should().Be(TimekeepingErrors.FixedTimingRequiresWindow);
    }

    [Fact]
    public void ShiftTimingInput_Fixed_FailsOnAnInvertedWindow()
    {
        var input = new ShiftTimingInput(
            ShiftTimingKind.Fixed, new TimeOnly(18, 0), new TimeOnly(9, 0), null, null, null, null, null);

        input.ToTiming().Error.Should().Be(TimekeepingErrors.TimeWindowEndNotAfterStart);
    }

    [Fact]
    public void ShiftTimingInput_Flexible_FailsWithoutCoreHours()
    {
        var input = new ShiftTimingInput(
            ShiftTimingKind.Flexible, null, null, new TimeOnly(7, 0), new TimeOnly(10, 0), null, null,
            TimeSpan.FromHours(8));

        input.ToTiming().Error.Should().Be(TimekeepingErrors.FlexibleTimingRequiresAllFlexibleFields);
    }

    [Fact]
    public void ShiftTimingInput_Flexible_SucceedsWhenComplete()
    {
        var input = new ShiftTimingInput(
            ShiftTimingKind.Flexible, null, null, new TimeOnly(7, 0), new TimeOnly(10, 0), new TimeOnly(10, 0),
            new TimeOnly(15, 0), TimeSpan.FromHours(8));

        input.ToTiming().IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void BreakRuleInput_ConvertsAFloatingBreakWithNoWindow()
    {
        var input = new BreakRuleInput(BreakKind.Rest, TimeSpan.FromMinutes(15), null, null, true, false);

        input.ToBreakRule().Window.Should().BeNull();
        input.ToBreakRule().Paid.Should().BeTrue();
    }

    [Fact]
    public void ShiftPeriodInput_Converts()
    {
        var input = new ShiftPeriodInput(1, new TimeOnly(8, 0), new TimeOnly(12, 0));

        input.ToShiftPeriod().Sequence.Should().Be(1);
    }
}
