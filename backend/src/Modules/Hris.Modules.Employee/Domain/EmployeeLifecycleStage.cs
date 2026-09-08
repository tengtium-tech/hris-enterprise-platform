namespace Hris.Modules.Employee.Domain;

/// <summary>
/// Source: docs/04-modules/employee/domain/business-rules.md BR-EMP-018's own
/// authoritative enumeration ("Lifecycle Stages: Candidate, Pre-Hire, Hired,
/// Onboarding, Active, Offboarding, Separated, Retired, Deceased"). Leave,
/// Suspended, Resigned, and Terminated are explicitly excluded from this list by
/// that same rule -- they are Operational Status values and separation reasons
/// owned by the Employment aggregate (ADR-0008), not Lifecycle Stages; an employee
/// remains <see cref="Active"/> throughout all four. This resolves an internal
/// inconsistency in employee-lifecycle.md, whose own diagram and "Lifecycle
/// Transitions" list still show Resigned/Terminated as if they were stages --
/// BR-EMP-018 is the numbered Business Rule and is treated as authoritative over
/// the stale diagram, the same "read files before judging them" resolution this
/// project's own CLAUDE.md documents for cross-document contradictions.
///
/// <see cref="Candidate"/> and <see cref="PreHire"/> are retained in the enum for
/// completeness with BR-EMP-018, but the <see cref="Employee"/> Aggregate does not
/// exist in either stage -- employee-lifecycle.md's own Candidate/Pre-Hire
/// descriptions state the individual is "Recruitment owned" with "No Employee
/// Number assigned" ; <see cref="Employee.Create"/> always starts a new instance at
/// <see cref="Hired"/>, matching "Hired: Employee Number assigned. Employee
/// Profile created."
/// </summary>
public enum EmployeeLifecycleStage
{
    Candidate = 0,
    PreHire = 1,
    Hired = 2,
    Onboarding = 3,
    Active = 4,
    Offboarding = 5,
    Separated = 6,
    Retired = 7,
    Deceased = 8,
}
