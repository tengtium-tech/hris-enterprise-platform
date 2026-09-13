namespace Hris.Modules.Attendance.Domain;

/// <summary>
/// The overtime ruleset an <see cref="AttendanceCalculationEngine"/> applies when it
/// reaches its overtime step, part of <see cref="AttendancePolicy"/>. Source:
/// docs/04-modules/attendance/domain/aggregates.md (AttendancePolicy owns
/// "overtime policy configuration — eligibility, prior-authorization requirement,
/// thresholds").
/// </summary>
public readonly record struct OvertimePolicyConfiguration(
    bool Eligible,
    bool RequiresPriorAuthorization,
    double DailyThresholdHours,
    double WeeklyThresholdHours);
