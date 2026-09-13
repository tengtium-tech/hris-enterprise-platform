namespace Hris.Modules.Attendance.Domain;

/// <summary>
/// Classifies an <see cref="OvertimeRequest"/> and the overtime hours a calculation
/// attributes to it, per docs/04-modules/attendance/domain/value-objects.md.
/// Attendance classifies; it does not price — payroll applies the premium rate.
/// </summary>
public enum OvertimeCategory
{
    Regular,
    RestDay,
    RegularHoliday,
    SpecialHoliday,
    Night,
    Emergency,
    Project,
    OnCall,
}
