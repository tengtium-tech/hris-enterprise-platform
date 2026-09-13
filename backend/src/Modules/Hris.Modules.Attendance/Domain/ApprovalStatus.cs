namespace Hris.Modules.Attendance.Domain;

/// <summary>
/// Review lifecycle shared by <see cref="AttendanceAdjustment"/> and
/// <see cref="OvertimeRequest"/>, per docs/04-modules/attendance/domain/aggregates.md.
/// </summary>
public enum ApprovalStatus
{
    Draft,
    Pending,
    Approved,
    Rejected,
    Cancelled,
}
