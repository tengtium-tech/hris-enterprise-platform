namespace Hris.Modules.Attendance.Domain;

/// <summary>
/// Grain at which an <see cref="AttendancePolicy"/> is assigned, per
/// docs/04-modules/attendance/domain/entities.md. The scope target identifier is a
/// reference into the organization or employee module, never duplicated text.
/// </summary>
public enum PolicyScopeLevel
{
    Company,
    LegalEntity,
    BusinessUnit,
    Department,
    Position,
    EmployeeGroup,
    EmploymentType,
    IndividualEmployee,
}
