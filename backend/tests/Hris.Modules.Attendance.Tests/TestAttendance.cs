using Hris.Modules.Attendance.Domain;

namespace Hris.Modules.Attendance.Tests;

/// <summary>
/// Shared fixtures, each building the minimum valid shape a test needs so a failure
/// points at the rule under test rather than at incidental setup.
/// </summary>
internal static class TestAttendance
{
    public static readonly DateTimeOffset NowUtc = new(2026, 9, 13, 9, 0, 0, TimeSpan.Zero);

    public static DateOnly Today => DateOnly.FromDateTime(NowUtc.UtcDateTime);

    public static AttendanceAdjustment Adjustment(
        Guid? tenantId = null,
        AttendanceRecordId? attendanceRecordId = null,
        DateOnly? workDate = null,
        string field = "ClockOut",
        string originalValue = "18:00",
        string requestedValue = "18:30",
        AdjustmentCategory category = AdjustmentCategory.ForgotClockOut,
        string reason = "Forgot to clock out",
        Guid? submittedBy = null) =>
        AttendanceAdjustment.Create(
            new AttendanceAdjustmentId(Guid.NewGuid()),
            tenantId ?? Guid.NewGuid(),
            attendanceRecordId ?? new AttendanceRecordId(Guid.NewGuid()),
            workDate ?? Today,
            field,
            originalValue,
            requestedValue,
            category,
            reason,
            null,
            submittedBy ?? Guid.NewGuid(),
            NowUtc).Value;

    /// <summary>A permissive default policy: no rounding, unpaid breaks, overtime eligible without prior authorization.</summary>
    public static PolicyCalculationConfiguration DefaultPolicy(
        bool overtimeEligible = true,
        bool requiresPriorAuthorization = false,
        bool breaksPaid = false,
        bool holidayPremiumApplies = true,
        RoundingRule roundingRule = RoundingRule.Exact,
        int graceMinutes = 0,
        int minimumBreakMinutes = 30,
        int maximumBreakMinutes = 90) =>
        new(
            WorkingHours: new WorkingHoursConfiguration(8, 12, 4, 40, null, null),
            GracePeriod: GracePeriod.FromMinutes(graceMinutes),
            RoundingRule: roundingRule,
            BreakPolicy: new BreakPolicyConfiguration(false, minimumBreakMinutes, maximumBreakMinutes, breaksPaid),
            OvertimePolicy: new OvertimePolicyConfiguration(overtimeEligible, requiresPriorAuthorization, 8, 40),
            HolidayRestDay: new HolidayRestDayConfiguration(holidayPremiumApplies, true));

    public static CalculationInputs Inputs(
        PolicyCalculationConfiguration? policy = null,
        Guid? resolvedWorkShiftId = null,
        TimeOnly? shiftStart = null,
        TimeOnly? shiftEnd = null,
        bool shiftUnresolved = false,
        bool shiftAmbiguous = false,
        bool isHoliday = false,
        string? holidayType = null,
        IReadOnlyList<OvertimeRequestSummary>? approvedOvertime = null) =>
        new(
            resolvedWorkShiftId ?? Guid.NewGuid(),
            shiftStart,
            shiftEnd,
            shiftUnresolved,
            shiftAmbiguous,
            isHoliday,
            holidayType,
            approvedOvertime ?? [],
            policy ?? DefaultPolicy());

    /// <summary>A real <see cref="AttendanceRecord"/> carrying the given captured events, built through
    /// the same public <see cref="AttendanceRecord.CaptureTimeEvent"/> path the command handler uses,
    /// rather than constructing <see cref="TimeEvent"/> directly.</summary>
    public static AttendanceRecord RecordWithEvents(
        Guid tenantId, Guid employeeId, DateOnly workDate, params (TimeEventType Type, DateTimeOffset At)[] events)
    {
        var record = AttendanceRecord.Create(
            new AttendanceRecordId(Guid.NewGuid()), tenantId, employeeId, workDate, NowUtc, null).Value;

        foreach (var (type, at) in events)
        {
            record.CaptureTimeEvent(
                new TimeEventId(Guid.NewGuid()), type, at, AttendanceSource.ManualEntry, null, null, null, NowUtc, null);
        }

        return record;
    }

    public static DateTimeOffset At(int hour, int minute = 0) =>
        new(Today, new TimeOnly(hour, minute), TimeSpan.Zero);
}
