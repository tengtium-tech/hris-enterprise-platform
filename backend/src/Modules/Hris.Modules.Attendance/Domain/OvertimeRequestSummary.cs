namespace Hris.Modules.Attendance.Domain;

/// <summary>
/// A flattened, Attendance-owned view of an approved <see cref="OvertimeRequest"/> the
/// calculation engine consults at its overtime step, when the effective
/// <see cref="AttendancePolicy"/> requires prior authorization (AT-040). Carries only
/// the planned window and category — Attendance classifies; payroll prices.
/// </summary>
public readonly record struct OvertimeRequestSummary(
    OvertimeRequestId Id,
    DateOnly WorkDate,
    TimeOnly? PlannedStart,
    TimeOnly? PlannedEnd,
    OvertimeCategory Category,
    double EstimatedHours);
