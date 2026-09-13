namespace Hris.Modules.Attendance.Domain;

/// <summary>
/// Outcome of one approval-chain step, recorded by <see cref="ApprovalDecision"/> on
/// <see cref="AttendanceRecord"/>, <see cref="AttendanceAdjustment"/>, and
/// <see cref="OvertimeRequest"/>, per docs/04-modules/attendance/domain/value-objects.md.
/// </summary>
public enum ApprovalDecisionOutcome
{
    Approved,
    Rejected,
    ReturnedForCorrection,
}
