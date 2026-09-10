namespace Hris.Modules.Timekeeping.Domain;

/// <summary>
/// The grain at which a schedule or shift assignment applies, doubling as the
/// precedence key. Source: docs/04-modules/timekeeping/domain/value-objects.md and
/// aggregates.md's own precedence list.
///
/// Ordinal order is significant and deliberately runs broadest to most specific, so
/// TK-030's "the most specific applicable assignment wins" is a comparison rather
/// than a lookup table. aggregates.md lists six precedence levels for shift
/// assignment (Individual Employee, Position, Department, Business Unit, Legal
/// Entity, Company); entities.md's ScheduleAssignment table adds EmploymentType and
/// EmployeeGroup, which sit between Position and Department in specificity because
/// each names a classification narrower than an organizational unit but broader than
/// one person. That placement is this module's own reconciliation of the two
/// documents rather than a value either states outright, and it is recorded here
/// rather than left implicit in a comparison.
///
/// Never encode a level into a schedule or shift name; scope is a separate value,
/// mirroring Administration's own OrganizationalScope.
/// </summary>
public enum OrganizationalAssignmentLevel
{
    Company = 0,
    LegalEntity = 1,
    BusinessUnit = 2,
    Department = 3,
    EmployeeGroup = 4,
    EmploymentType = 5,
    Position = 6,
    IndividualEmployee = 7,
}
