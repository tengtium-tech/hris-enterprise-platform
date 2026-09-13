namespace Hris.Modules.Attendance.Domain;

/// <summary>
/// Time-rounding behavior, part of <see cref="AttendancePolicy"/>, per
/// docs/04-modules/attendance/domain/value-objects.md. Applied uniformly to every
/// event in a record's calculation; the rounded value is an output, never a mutation
/// of the original captured timestamp (AT-004).
/// </summary>
public enum RoundingRule
{
    Exact,
    NearestMinute,
    NearestFiveMinutes,
    NearestFifteenMinutes,
    NearestThirtyMinutes,
}
