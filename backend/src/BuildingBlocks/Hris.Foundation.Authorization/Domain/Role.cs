namespace Hris.Foundation.Authorization.Domain;

/// <summary>
/// The ten canonical roles, defined authoritatively in
/// docs/00-project/personas.md (DOC-012) and restated in
/// authorization-framework.md's own Core Concepts section -- confirmed to match by
/// direct read of personas.md's own role table before writing this type, not assumed
/// from the restatement (per this project's own review discipline: "Check the
/// transcript... Read files before judging them").
///
/// Deliberately a C# <c>enum</c>, not a database-driven configurable list:
/// this project's engineering conventions state "Canonical roles are platform
/// vocabulary; modules reason about them," and personas.md itself: "New roles require
/// a change to DOC-012." An enum makes `CTR-AUT-001` ("No Role Name Comparison in
/// Code") mechanically true rather than merely policed: comparing a <see cref="Role"/>
/// to a string literal is a compile error, not a discouraged pattern, closing off
/// the exact anti-pattern this document's own Centralized Evaluation section shows --
/// <c>if (user.Role == "HR Manager")</c> -- at the type system rather than at review
/// time.
///
/// Organizational level is never encoded here (authorization-framework.md: "A
/// department-level HR officer is the `HROfficer` role granted at `Department`
/// scope, not a distinct role") -- see <see cref="OrganizationalScope"/>.
///
/// Platform Operator is deliberately absent. It is a real, separate role
/// (docs/00-project/platform-operations-roles.md, DOC-016), but `ADR-0009` decided
/// it explicitly outside this ten-role model: "Never grant a Platform Operator
/// permission to any tenant-scoped role, and never grant any tenant-scoped module's
/// permission to a Platform Operator account -- ADR-0009's boundary is bidirectional,"
/// and a Platform Operator account "never resolves a TenantId." Adding it to this
/// enum would let a <see cref="RoleAssignment"/> or a permission grant conflate the
/// two by construction -- exactly what that boundary forbids. Platform Operator's
/// own authorization routes through Tenant Framework (Sprint 4), not here.
/// </summary>
public enum Role
{
    SystemAdministrator = 0,
    HRAdministrator,
    HRManager,
    HROfficer,
    PayrollOfficer,
    Recruiter,
    PeopleManager,
    Employee,
    Executive,
    Auditor,
}
