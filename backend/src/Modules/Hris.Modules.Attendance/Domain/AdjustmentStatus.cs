namespace Hris.Modules.Attendance.Domain;

/// <summary>
/// Seven-state lifecycle of an <see cref="AttendanceAdjustment"/>, per
/// docs/04-modules/attendance/domain/aggregates.md. <see cref="Applied"/> is terminal
/// and reached only after <see cref="AttendanceRecord"/> confirms incorporation of the
/// approved change (AT-024).
/// </summary>
public enum AdjustmentStatus
{
    Draft,
    Submitted,
    UnderReview,
    Approved,
    Rejected,
    Cancelled,
    Applied,
}
