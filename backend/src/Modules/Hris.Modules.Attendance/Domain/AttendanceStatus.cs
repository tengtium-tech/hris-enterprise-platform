namespace Hris.Modules.Attendance.Domain;

/// <summary>
/// Lifecycle of an <see cref="AttendanceRecord"/>, per
/// docs/04-modules/attendance/domain/attendance-records.md's "AttendanceRecord Lifecycle".
/// A record moves Created -> Validated -> Calculated -> Submitted -> Approved ->
/// Finalized, and may return to Calculated via an authorized reopening.
/// </summary>
public enum AttendanceStatus
{
    Created,
    Validated,
    Calculated,
    Submitted,
    Approved,
    Finalized,
    Archived,
}
