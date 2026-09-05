namespace Hris.Foundation.WorkflowEngine.Domain;

/// <summary>
/// workflow-engine.md's own Workflow Participant section, verbatim closed list:
/// "Role-based participants: Employee, PeopleManager, HROfficer, HRManager,
/// PayrollOfficer, Executive." Deliberately narrower than `../00-project/personas.md`'s
/// own full canonical role list (which also names <c>SystemAdministrator</c> and
/// <c>HRAdministrator</c>) -- this framework's own document enumerates which of those
/// platform-wide roles may act as a workflow participant, and administrative roles are
/// not among them, the same "an implementer must not silently widen a document's own
/// explicit enumeration" discipline every other closed-vocabulary check in this
/// codebase already follows.
/// </summary>
public static class WorkflowCanonicalParticipantRoles
{
    public static readonly IReadOnlySet<string> Names = new HashSet<string>(StringComparer.Ordinal)
    {
        "Employee",
        "PeopleManager",
        "HROfficer",
        "HRManager",
        "PayrollOfficer",
        "Executive",
    };
}
