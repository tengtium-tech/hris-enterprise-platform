namespace Hris.Foundation.Authorization.Domain;

/// <summary>
/// The four ways a principal receives a role, per authorization-framework.md's Role
/// Assignment section: "Direct Role Assignment, Organizational Role Assignment,
/// Temporary Role Assignment, Delegated Role Assignment."
/// </summary>
public enum RoleAssignmentType
{
    Direct = 0,
    Organizational,
    Temporary,
    Delegated,
}
