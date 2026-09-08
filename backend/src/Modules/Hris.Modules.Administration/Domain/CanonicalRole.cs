namespace Hris.Modules.Administration.Domain;

/// <summary>
/// The platform's canonical role vocabulary. Source: docs/00-project/personas.md
/// (DOC-012) Section 4, "Canonical Roles" -- "authoritative. Module documentation,
/// permission definitions, workflow approver definitions, and generated code must
/// use these names exactly." Hardcoded here rather than resolved through a
/// cross-module reference, since DOC-012 states these names are platform
/// vocabulary, not data any module owns or could vary at runtime -- the identical
/// reasoning already applied to this platform's other closed, doc-defined
/// enumerations (e.g. Employee's own <c>EmployeeLifecycleStage</c> against
/// BR-EMP-018).
/// </summary>
public enum CanonicalRole
{
    SystemAdministrator = 0,
    HRAdministrator = 1,
    HRManager = 2,
    HROfficer = 3,
    PayrollOfficer = 4,
    Recruiter = 5,
    PeopleManager = 6,
    Employee = 7,
    Executive = 8,
    Auditor = 9,
}
