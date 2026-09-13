namespace Hris.Modules.Attendance.Domain;

/// <summary>
/// The full set of <see cref="AttendancePolicy"/> values the
/// <see cref="AttendanceCalculationEngine"/> evaluates a record against, bundled so the
/// engine consumes one Attendance-owned type rather than the policy aggregate directly.
/// The <c>RunCalculationCommand</c> handler extracts these from the policy version
/// effective on the work date (AT-002) and passes them in — the engine never reaches
/// across the module boundary to Timekeeping or to the policy's own tables.
/// </summary>
public readonly record struct PolicyCalculationConfiguration(
    WorkingHoursConfiguration WorkingHours,
    GracePeriod GracePeriod,
    RoundingRule RoundingRule,
    BreakPolicyConfiguration BreakPolicy,
    OvertimePolicyConfiguration OvertimePolicy,
    HolidayRestDayConfiguration HolidayRestDay);
