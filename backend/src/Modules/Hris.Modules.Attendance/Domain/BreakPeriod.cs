using Hris.SharedKernel;

namespace Hris.Modules.Attendance.Domain;

/// <summary>
/// A calculated pairing of a Break-Start and its matching Break-End event, produced by
/// <see cref="AttendanceCalculationEngine"/> — never independently persisted (AT-003).
/// Duration, paid/unpaid classification, and validity are all derived at calculation
/// time from the effective <see cref="AttendancePolicy"/>, per
/// docs/04-modules/attendance/domain/value-objects.md.
/// </summary>
public sealed record BreakPeriod(
    TimeEventId StartEventId,
    TimeEventId EndEventId,
    TimeSpan Duration,
    bool Paid,
    bool Valid);

/// <summary>One payroll-relevant derived total produced by the calculation engine.</summary>
public sealed record CalculatedFields(
    double WorkingHours,
    double PayableHours,
    double OvertimeHours,
    double LateMinutes,
    double UndertimeMinutes,
    double HolidayHours,
    double NightDifferentialHours);
