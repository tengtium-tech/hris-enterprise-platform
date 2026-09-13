namespace Hris.Modules.Leave.Domain;

/// <summary>
/// Organizational scope a <see cref="PolicyAssignment"/> binds a <see cref="LeavePolicy"/>
/// to. Resolution precedence (most specific first) is a caller-supplied ordering of
/// candidate target identifiers, not an ordinal property of this enum — the identical
/// approach <c>Hris.Modules.Attendance</c>'s own effective-policy resolution uses.
/// Source: docs/04-modules/leave/domain/leave-policies.md.
/// </summary>
public enum LeavePolicyScopeLevel
{
    Company,
    LegalEntity,
    BusinessUnit,
    Department,
    EmploymentType,
    EmployeeGroup,
    Position,
    Employee,
}
