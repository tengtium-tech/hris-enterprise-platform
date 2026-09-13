using FluentAssertions;
using Hris.Modules.Attendance.Domain;
using Xunit;

namespace Hris.Modules.Attendance.Tests.Domain;

/// <summary>
/// BUS-070's fifteen-step deterministic pipeline. The engine is stateless and pure —
/// same event set, same <see cref="CalculationInputs"/>, same <see cref="CalculationResult"/>
/// every time (AT-003) — so every test here constructs its own events and inputs
/// directly rather than sharing mutable fixtures across cases.
///
/// One test (<see cref="Calculate_FlagsAbsence_WhenShiftResolvedButNoClockInRecorded"/>)
/// exists because it failed against the engine as first written: the absence-detection
/// check (step 13) was nested inside a block gated on a clock-in already being present,
/// while itself checking for the clock-in's absence — unreachable by construction. Fixed
/// alongside this test, not filed separately, since the test could not otherwise be
/// written meaningfully.
/// </summary>
public sealed class AttendanceCalculationEngineTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();
    private static readonly Guid _employeeId = Guid.NewGuid();
    private static DateOnly WorkDate => TestAttendance.Today;

    private static IReadOnlyList<TimeEvent> Events(params (TimeEventType Type, DateTimeOffset At)[] events) =>
        TestAttendance.RecordWithEvents(_tenantId, _employeeId, WorkDate, events).TimeEvents;

    // ---- No events / unresolved shift ---------------------------------

    [Fact]
    public void Calculate_FlagsException_WhenNoTimeEventsCaptured()
    {
        var result = AttendanceCalculationEngine.Calculate(
            [], WorkDate, TestAttendance.Inputs(), TestAttendance.NowUtc);

        result.Exceptions.Should().Contain(e => e.Contains("No time events captured", StringComparison.Ordinal));
        result.Fields.WorkingHours.Should().Be(0);
    }

    [Fact]
    public void Calculate_FlagsException_WhenShiftUnresolved()
    {
        var events = Events((TimeEventType.ClockIn, TestAttendance.At(9)), (TimeEventType.ClockOut, TestAttendance.At(17)));

        var result = AttendanceCalculationEngine.Calculate(
            events, WorkDate, TestAttendance.Inputs(shiftUnresolved: true), TestAttendance.NowUtc);

        result.Exceptions.Should().Contain(e => e.Contains("Shift could not be resolved", StringComparison.Ordinal));
    }

    [Fact]
    public void Calculate_FlagsAbsence_WhenShiftResolvedButNoClockInRecorded()
    {
        var inputs = TestAttendance.Inputs(shiftStart: new TimeOnly(9, 0), shiftEnd: new TimeOnly(18, 0));

        var result = AttendanceCalculationEngine.Calculate([], WorkDate, inputs, TestAttendance.NowUtc);

        result.Exceptions.Should().Contain(e => e.Contains("No clock-in on a scheduled work date", StringComparison.Ordinal));
    }

    [Fact]
    public void Calculate_DoesNotFlagAbsence_WhenShiftIsNotResolved()
    {
        // No shift means nothing was scheduled to compare against -- silence, not a false absence.
        var result = AttendanceCalculationEngine.Calculate([], WorkDate, TestAttendance.Inputs(), TestAttendance.NowUtc);

        result.Exceptions.Should().NotContain(e => e.Contains("absence", StringComparison.OrdinalIgnoreCase));
    }

    // ---- Working and payable hours -------------------------------------

    [Fact]
    public void Calculate_ComputesWorkingHours_FromClockInToClockOut()
    {
        var events = Events((TimeEventType.ClockIn, TestAttendance.At(9)), (TimeEventType.ClockOut, TestAttendance.At(17)));

        var result = AttendanceCalculationEngine.Calculate(events, WorkDate, TestAttendance.Inputs(), TestAttendance.NowUtc);

        result.Fields.WorkingHours.Should().Be(8);
    }

    [Fact]
    public void Calculate_DeductsUnpaidBreaks_FromPayableHoursOnly()
    {
        var events = Events(
            (TimeEventType.ClockIn, TestAttendance.At(9)),
            (TimeEventType.BreakStart, TestAttendance.At(12)),
            (TimeEventType.BreakEnd, TestAttendance.At(12, 30)),
            (TimeEventType.ClockOut, TestAttendance.At(17)));
        var inputs = TestAttendance.Inputs(policy: TestAttendance.DefaultPolicy(breaksPaid: false));

        var result = AttendanceCalculationEngine.Calculate(events, WorkDate, inputs, TestAttendance.NowUtc);

        result.Fields.WorkingHours.Should().Be(8, "the break window still falls inside clock-in to clock-out");
        result.Fields.PayableHours.Should().Be(7.5, "a 30-minute unpaid break is deducted from payable hours only");
    }

    [Fact]
    public void Calculate_DoesNotDeductPaidBreaks_FromPayableHours()
    {
        var events = Events(
            (TimeEventType.ClockIn, TestAttendance.At(9)),
            (TimeEventType.BreakStart, TestAttendance.At(12)),
            (TimeEventType.BreakEnd, TestAttendance.At(12, 30)),
            (TimeEventType.ClockOut, TestAttendance.At(17)));
        var inputs = TestAttendance.Inputs(policy: TestAttendance.DefaultPolicy(breaksPaid: true));

        var result = AttendanceCalculationEngine.Calculate(events, WorkDate, inputs, TestAttendance.NowUtc);

        result.Fields.PayableHours.Should().Be(8);
    }

    [Theory]
    [InlineData(15, 10)] // break shorter than the configured minimum
    [InlineData(120, 90)] // break longer than the configured maximum
    public void Calculate_FlagsException_WhenBreakDurationOutsideConfiguredRange(int actualMinutes, int maximumMinutes)
    {
        var events = Events(
            (TimeEventType.ClockIn, TestAttendance.At(9)),
            (TimeEventType.BreakStart, TestAttendance.At(12)),
            (TimeEventType.BreakEnd, TestAttendance.At(12).AddMinutes(actualMinutes)),
            (TimeEventType.ClockOut, TestAttendance.At(17)));
        var inputs = TestAttendance.Inputs(
            policy: TestAttendance.DefaultPolicy(minimumBreakMinutes: 30, maximumBreakMinutes: maximumMinutes));

        var result = AttendanceCalculationEngine.Calculate(events, WorkDate, inputs, TestAttendance.NowUtc);

        result.Exceptions.Should().Contain(e => e.Contains("Break duration outside", StringComparison.Ordinal));
    }

    [Fact]
    public void Calculate_FlagsException_WhenBreakEndHasNoMatchingStart()
    {
        var events = Events(
            (TimeEventType.ClockIn, TestAttendance.At(9)),
            (TimeEventType.BreakEnd, TestAttendance.At(12, 30)),
            (TimeEventType.ClockOut, TestAttendance.At(17)));

        var result = AttendanceCalculationEngine.Calculate(events, WorkDate, TestAttendance.Inputs(), TestAttendance.NowUtc);

        result.Exceptions.Should().Contain(e => e.Contains("Break end without a matching break start", StringComparison.Ordinal));
    }

    [Fact]
    public void Calculate_FlagsException_WhenBreakStartHasNoMatchingEnd()
    {
        var events = Events(
            (TimeEventType.ClockIn, TestAttendance.At(9)),
            (TimeEventType.BreakStart, TestAttendance.At(12)),
            (TimeEventType.ClockOut, TestAttendance.At(17)));

        var result = AttendanceCalculationEngine.Calculate(events, WorkDate, TestAttendance.Inputs(), TestAttendance.NowUtc);

        result.Exceptions.Should().Contain(e => e.Contains("Break start without a matching break end", StringComparison.Ordinal));
    }

    // ---- Rounding ------------------------------------------------------

    // Every row rounds down to a clean 8.0 hours, deliberately -- the engine's final
    // step always rounds the whole result to 2 decimal places regardless of which
    // RoundingRule applied first (see Round(value) at the end of Calculate), so a raw
    // value producing an already-clean nearest-N-minutes result avoids that second
    // rounding pass introducing an unrelated discrepancy into the assertion. Also
    // deliberately not a value sitting exactly on a rounding midpoint (8h15m against a
    // 30-minute rule) -- Math.Round's AwayFromZero midpoint behavior rounds a true
    // midpoint UP, easy to get backwards when picking a "clean-looking" test value.
    [Theory]
    [InlineData(RoundingRule.NearestFiveMinutes, 482)] // 8h02m -> nearest 5 min is 8h00m
    [InlineData(RoundingRule.NearestFifteenMinutes, 485)] // 8h05m -> nearest 15 min is 8h00m
    [InlineData(RoundingRule.NearestThirtyMinutes, 490)] // 8h10m -> nearest 30 min is 8h00m
    [InlineData(RoundingRule.Exact, 480)] // already exactly 8h00m; untouched
    public void Calculate_AppliesTheConfiguredRoundingRule_ToWorkingHours(RoundingRule rule, int rawMinutes)
    {
        var clockOut = TestAttendance.At(9).AddMinutes(rawMinutes);
        var events = Events((TimeEventType.ClockIn, TestAttendance.At(9)), (TimeEventType.ClockOut, clockOut));
        var inputs = TestAttendance.Inputs(policy: TestAttendance.DefaultPolicy(roundingRule: rule));

        var result = AttendanceCalculationEngine.Calculate(events, WorkDate, inputs, TestAttendance.NowUtc);

        result.Fields.WorkingHours.Should().Be(8.0);
    }

    // ---- Tardiness and undertime ---------------------------------------

    [Fact]
    public void Calculate_ComputesLateMinutes_WhenClockInIsAfterTheGracePeriod()
    {
        var events = Events((TimeEventType.ClockIn, TestAttendance.At(9, 20)), (TimeEventType.ClockOut, TestAttendance.At(17)));
        var inputs = TestAttendance.Inputs(
            shiftStart: new TimeOnly(9, 0), shiftEnd: new TimeOnly(18, 0),
            policy: TestAttendance.DefaultPolicy(graceMinutes: 10));

        var result = AttendanceCalculationEngine.Calculate(events, WorkDate, inputs, TestAttendance.NowUtc);

        result.Fields.LateMinutes.Should().Be(10, "20 minutes late minus a 10-minute grace period");
    }

    [Fact]
    public void Calculate_ComputesNoLateMinutes_WhenClockInIsWithinTheGracePeriod()
    {
        var events = Events((TimeEventType.ClockIn, TestAttendance.At(9, 5)), (TimeEventType.ClockOut, TestAttendance.At(17)));
        var inputs = TestAttendance.Inputs(
            shiftStart: new TimeOnly(9, 0), policy: TestAttendance.DefaultPolicy(graceMinutes: 10));

        var result = AttendanceCalculationEngine.Calculate(events, WorkDate, inputs, TestAttendance.NowUtc);

        result.Fields.LateMinutes.Should().Be(0);
    }

    [Fact]
    public void Calculate_ComputesUndertimeMinutes_WhenClockOutIsBeforeShiftEnd()
    {
        var events = Events((TimeEventType.ClockIn, TestAttendance.At(9)), (TimeEventType.ClockOut, TestAttendance.At(17, 30)));
        var inputs = TestAttendance.Inputs(shiftStart: new TimeOnly(9, 0), shiftEnd: new TimeOnly(18, 0));

        var result = AttendanceCalculationEngine.Calculate(events, WorkDate, inputs, TestAttendance.NowUtc);

        result.Fields.UndertimeMinutes.Should().Be(30);
    }

    // ---- Overtime --------------------------------------------------------

    [Fact]
    public void Calculate_ComputesOvertimeHours_WhenClockOutIsAfterShiftEnd_AndPolicyIsEligible()
    {
        var events = Events((TimeEventType.ClockIn, TestAttendance.At(9)), (TimeEventType.ClockOut, TestAttendance.At(19)));
        var inputs = TestAttendance.Inputs(
            shiftStart: new TimeOnly(9, 0), shiftEnd: new TimeOnly(18, 0),
            policy: TestAttendance.DefaultPolicy(overtimeEligible: true, requiresPriorAuthorization: false));

        var result = AttendanceCalculationEngine.Calculate(events, WorkDate, inputs, TestAttendance.NowUtc);

        result.Fields.OvertimeHours.Should().Be(1);
    }

    [Fact]
    public void Calculate_ZeroesOvertimeAndFlagsException_WhenThePolicyIsNotOvertimeEligible()
    {
        var events = Events((TimeEventType.ClockIn, TestAttendance.At(9)), (TimeEventType.ClockOut, TestAttendance.At(19)));
        var inputs = TestAttendance.Inputs(
            shiftStart: new TimeOnly(9, 0), shiftEnd: new TimeOnly(18, 0),
            policy: TestAttendance.DefaultPolicy(overtimeEligible: false));

        var result = AttendanceCalculationEngine.Calculate(events, WorkDate, inputs, TestAttendance.NowUtc);

        result.Fields.OvertimeHours.Should().Be(0);
        result.Exceptions.Should().Contain(e => e.Contains("not overtime-eligible", StringComparison.Ordinal));
    }

    [Fact]
    public void Calculate_FlagsException_WhenOvertimeRequiresPriorAuthorizationAndNoneIsApproved()
    {
        var events = Events((TimeEventType.ClockIn, TestAttendance.At(9)), (TimeEventType.ClockOut, TestAttendance.At(19)));
        var inputs = TestAttendance.Inputs(
            shiftStart: new TimeOnly(9, 0), shiftEnd: new TimeOnly(18, 0),
            policy: TestAttendance.DefaultPolicy(requiresPriorAuthorization: true));

        var result = AttendanceCalculationEngine.Calculate(events, WorkDate, inputs, TestAttendance.NowUtc);

        result.Exceptions.Should().Contain(e => e.Contains("exceeds approved authorization", StringComparison.Ordinal));
    }

    [Fact]
    public void Calculate_DoesNotFlagException_WhenApprovedOvertimeCoversTheWorkDate()
    {
        var events = Events((TimeEventType.ClockIn, TestAttendance.At(9)), (TimeEventType.ClockOut, TestAttendance.At(19)));
        var approved = new OvertimeRequestSummary(
            new OvertimeRequestId(Guid.NewGuid()), WorkDate, new TimeOnly(18, 0), new TimeOnly(19, 0),
            OvertimeCategory.Regular, EstimatedHours: 1);
        var inputs = TestAttendance.Inputs(
            shiftStart: new TimeOnly(9, 0), shiftEnd: new TimeOnly(18, 0),
            policy: TestAttendance.DefaultPolicy(requiresPriorAuthorization: true),
            approvedOvertime: [approved]);

        var result = AttendanceCalculationEngine.Calculate(events, WorkDate, inputs, TestAttendance.NowUtc);

        result.Exceptions.Should().NotContain(e => e.Contains("exceeds approved authorization", StringComparison.Ordinal));
    }

    [Fact]
    public void Calculate_AddsApprovedOvertimeHours_IntoPayableHours()
    {
        var events = Events((TimeEventType.ClockIn, TestAttendance.At(9)), (TimeEventType.ClockOut, TestAttendance.At(18)));
        var approved = new OvertimeRequestSummary(
            new OvertimeRequestId(Guid.NewGuid()), WorkDate, null, null, OvertimeCategory.Regular, EstimatedHours: 2);
        var inputs = TestAttendance.Inputs(shiftStart: new TimeOnly(9, 0), shiftEnd: new TimeOnly(18, 0), approvedOvertime: [approved]);

        var result = AttendanceCalculationEngine.Calculate(events, WorkDate, inputs, TestAttendance.NowUtc);

        result.Fields.PayableHours.Should().Be(11, "9 worked hours plus 2 separately approved overtime hours");
    }

    // ---- Holiday ---------------------------------------------------------

    [Fact]
    public void Calculate_AttributesWorkingHoursToHolidayHours_WhenTheDateIsAHoliday()
    {
        var events = Events((TimeEventType.ClockIn, TestAttendance.At(9)), (TimeEventType.ClockOut, TestAttendance.At(17)));
        var inputs = TestAttendance.Inputs(isHoliday: true, holidayType: "RegularHoliday");

        var result = AttendanceCalculationEngine.Calculate(events, WorkDate, inputs, TestAttendance.NowUtc);

        result.Fields.HolidayHours.Should().Be(8);
    }

    [Fact]
    public void Calculate_FlagsException_WhenWorkedOnAHoliday_ButThePolicyAppliesNoPremium()
    {
        var events = Events((TimeEventType.ClockIn, TestAttendance.At(9)), (TimeEventType.ClockOut, TestAttendance.At(17)));
        var inputs = TestAttendance.Inputs(
            isHoliday: true, policy: TestAttendance.DefaultPolicy(holidayPremiumApplies: false));

        var result = AttendanceCalculationEngine.Calculate(events, WorkDate, inputs, TestAttendance.NowUtc);

        result.Exceptions.Should().Contain(e => e.Contains("does not apply a holiday premium", StringComparison.Ordinal));
    }

    [Fact]
    public void Calculate_ReportsZeroHolidayHours_WhenTheDateIsNotAHoliday()
    {
        var events = Events((TimeEventType.ClockIn, TestAttendance.At(9)), (TimeEventType.ClockOut, TestAttendance.At(17)));

        var result = AttendanceCalculationEngine.Calculate(events, WorkDate, TestAttendance.Inputs(isHoliday: false), TestAttendance.NowUtc);

        result.Fields.HolidayHours.Should().Be(0);
    }

    // ---- Reproducibility (AT-003) ----------------------------------------

    [Fact]
    public void Calculate_ProducesTheIdenticalResult_WhenRunTwiceOverTheSameInputs()
    {
        // AT-003: the same event set and the same inputs always reproduce the same result.
        var events = Events(
            (TimeEventType.ClockIn, TestAttendance.At(9)),
            (TimeEventType.BreakStart, TestAttendance.At(12)),
            (TimeEventType.BreakEnd, TestAttendance.At(12, 30)),
            (TimeEventType.ClockOut, TestAttendance.At(19)));
        var inputs = TestAttendance.Inputs(
            shiftStart: new TimeOnly(9, 0), shiftEnd: new TimeOnly(18, 0), isHoliday: true);

        var first = AttendanceCalculationEngine.Calculate(events, WorkDate, inputs, TestAttendance.NowUtc);
        var second = AttendanceCalculationEngine.Calculate(events, WorkDate, inputs, TestAttendance.NowUtc.AddDays(3));

        second.Fields.Should().Be(first.Fields, "the same events and inputs must reproduce the same result regardless of when the run happens");
    }
}
