namespace Hris.Modules.Attendance.Domain;

/// <summary>
/// The break ruleset an <see cref="AttendanceCalculationEngine"/> uses to pair a
/// Break-Start with its Break-End and classify the resulting <see cref="BreakPeriod"/>
/// (paid/unpaid, valid), part of <see cref="AttendancePolicy"/>. Source:
/// docs/04-modules/attendance/domain/aggregates.md (AttendancePolicy owns
/// "break policy configuration — required breaks, paid/unpaid status, min/max
/// duration").
/// </summary>
public readonly record struct BreakPolicyConfiguration(
    bool RequiresBreak,
    int MinimumDurationMinutes,
    int MaximumDurationMinutes,
    bool BreaksPaid);
