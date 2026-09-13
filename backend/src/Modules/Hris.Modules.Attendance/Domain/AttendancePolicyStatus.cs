namespace Hris.Modules.Attendance.Domain;

/// <summary>
/// Lifecycle of an <see cref="AttendancePolicy"/> version. A version is authored as
/// <see cref="Draft"/>, becomes <see cref="Active"/> on publication with an effective
/// date, and is <see cref="Retired"/> when superseded — never edited in place
/// (AT-002). Source: docs/04-modules/attendance/domain/aggregates.md (AttendancePolicy).
/// </summary>
public enum AttendancePolicyStatus
{
    Draft,
    Active,
    Retired,
}
