namespace Hris.Modules.Attendance.Domain;

/// <summary>
/// Type of a captured <see cref="TimeEvent"/>, per
/// docs/04-modules/attendance/domain/value-objects.md. Platform baseline plus
/// tenant-configurable extensions; a type absent from the effective policy's configured
/// vocabulary is rejected at capture.
/// </summary>
public enum TimeEventType
{
    ClockIn,
    ClockOut,
    BreakStart,
    BreakEnd,
    MealStart,
    MealEnd,
    OvertimeStart,
    OvertimeEnd,
}
