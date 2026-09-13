namespace Hris.Modules.Attendance.Domain;

/// <summary>
/// The working-hours ruleset an <see cref="AttendanceCalculationEngine"/> evaluates
/// every record against, part of <see cref="AttendancePolicy"/>. Source:
/// docs/04-modules/attendance/domain/aggregates.md (AttendancePolicy owns
/// "working-hours configuration — standard/maximum/minimum daily and weekly hours,
/// core hours").
/// </summary>
public readonly record struct WorkingHoursConfiguration(
    double StandardDailyHours,
    double MaximumDailyHours,
    double MinimumDailyHours,
    double StandardWeeklyHours,
    TimeOnly? CoreHoursStart,
    TimeOnly? CoreHoursEnd);
