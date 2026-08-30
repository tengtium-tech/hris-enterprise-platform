using Hris.Foundation.Identity.Domain;
using Hris.SharedKernel;

namespace Hris.Foundation.Authorization.Domain;

/// <summary>
/// Six of authorization-framework.md's own ten listed Domain Events.
/// <c>RoleCreated</c>/<c>RoleUpdated</c> and <c>PolicyCreated</c>/<c>PolicyUpdated</c>
/// are deliberately not implemented here: <see cref="Role"/> is a fixed platform enum
/// with no runtime "create a role" operation (personas.md: "New roles require a
/// change to DOC-012," a documentation and deployment change, not a domain
/// operation), and full Policy/ABAC evaluation is an extension point this Sprint 3
/// pass defers to the Rules Engine, built later in this same Sprint -- see
/// <see cref="AuthorizationEvaluator"/>'s own remarks. Raising either pair now would
/// describe an aggregate this framework does not build.
/// </summary>
public sealed record RoleAssigned(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    RoleAssignmentId RoleAssignmentId,
    UserAccountId PrincipalId,
    Role Role,
    OrganizationalScope Scope) : IDomainEvent;

public sealed record RoleRevoked(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    RoleAssignmentId RoleAssignmentId,
    UserAccountId PrincipalId,
    Role Role) : IDomainEvent;

public sealed record PermissionGranted(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    RolePermissionGrantId GrantId,
    Role Role,
    PermissionKey Permission) : IDomainEvent;

public sealed record PermissionRevoked(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    RolePermissionGrantId GrantId,
    Role Role,
    PermissionKey Permission) : IDomainEvent;

public sealed record AuthorizationEvaluated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    UserAccountId PrincipalId,
    PermissionKey RequestedPermission,
    bool IsAllowed) : IDomainEvent;

public sealed record AuthorizationDenied(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    UserAccountId PrincipalId,
    PermissionKey RequestedPermission,
    string Reason) : IDomainEvent;
