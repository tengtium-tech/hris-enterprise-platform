namespace Hris.Modules.Timekeeping.Domain;

/// <summary>Source: docs/04-modules/timekeeping/domain/value-objects.md.</summary>
public enum WorkScheduleStatus
{
    Draft = 0,
    Active = 1,

    /// <summary>Replaced by a later version. Retained unedited forever (TK-001).</summary>
    Superseded = 2,

    Retired = 3,
}

/// <summary>Source: docs/04-modules/timekeeping/domain/value-objects.md.</summary>
public enum WorkShiftStatus
{
    Draft = 0,
    Active = 1,
    Superseded = 2,
    Retired = 3,
}

/// <summary>Source: docs/04-modules/timekeeping/domain/value-objects.md.</summary>
public enum ShiftAssignmentStatus
{
    Scheduled = 0,
    Active = 1,

    /// <summary>Replaced through a shift swap; linked to its counterpart (TK-033).</summary>
    Swapped = 2,

    /// <summary>Reached its end date automatically, with no administrative action (TK-032).</summary>
    Expired = 3,

    Cancelled = 4,
}

/// <summary>Source: docs/04-modules/timekeeping/domain/value-objects.md.</summary>
public enum HolidayCalendarStatus
{
    Draft = 0,
    Published = 1,
    Superseded = 2,
    Retired = 3,
}

/// <summary>
/// The layering order holiday resolution walks. Source:
/// docs/04-modules/timekeeping/domain/aggregates.md's own layering diagram. Ordinal
/// order is significant: a higher value is a more specific layer, and TK-042
/// resolves a conflict to the most specific applicable layer.
/// </summary>
public enum HolidayCalendarLevel
{
    /// <summary>Platform-provided statutory layer. Read-only to the tenant (TK-041).</summary>
    Country = 0,

    Region = 1,
    Company = 2,
}

/// <summary>Source: docs/04-modules/timekeeping/domain/value-objects.md.</summary>
public enum AssignmentTargetType
{
    Employee = 0,
    OrganizationalUnit = 1,
}

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

/// <summary>
/// Country-scoped holiday classification. Source:
/// docs/04-modules/timekeeping/domain/value-objects.md.
///
/// The three-way statutory split is the Philippine DOLE classification
/// specifically, not a universal taxonomy. A future country configuration may define
/// an entirely different set; TK-043 validates a value against the vocabulary
/// defined for the calendar's own country rather than assuming this set applies
/// everywhere.
/// </summary>
public enum HolidayType
{
    RegularHoliday = 0,
    SpecialNonWorkingHoliday = 1,
    SpecialWorkingHoliday = 2,
    LocalHoliday = 3,
    CompanyHoliday = 4,
    FloatingHoliday = 5,
}

/// <summary>
/// Whether a scheduled work day falling on a holiday still expects attendance. The
/// premium and pay consequence of each value is computed by payroll; this states
/// only the expectation.
/// </summary>
public enum HolidayWorkRule
{
    NoWorkExpected = 0,
    WorkVoluntary = 1,
    WorkRequired = 2,
}

/// <summary>How a shift's daily hours are determined.</summary>
public enum ShiftTimingKind
{
    Fixed = 0,
    Flexible = 1,
}

/// <summary>
/// Which end of an overnight shift determines the single work date its hours belong
/// to. The platform default is <see cref="ShiftStart"/> — a 22:00 to 06:00 shift
/// belongs to the date it starts on.
/// </summary>
public enum WorkDateAnchorPoint
{
    ShiftStart = 0,
    ShiftEnd = 1,
}

/// <summary>Source: docs/04-modules/timekeeping/domain/value-objects.md's BreakPeriod table.</summary>
public enum BreakKind
{
    Meal = 0,
    Rest = 1,
    Prayer = 2,
    CompanyDefined = 3,
}
