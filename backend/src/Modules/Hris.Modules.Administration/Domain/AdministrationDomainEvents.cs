using Hris.SharedKernel;

namespace Hris.Modules.Administration.Domain;

/// <summary>
/// Every Domain Event this module's three Aggregate Roots raise. Source:
/// docs/04-modules/administration/domain/domain-events.md, which names each of
/// these by section. <see cref="Guid"/> is used for every actor reference
/// (<c>GrantedBy</c>, <c>RevokedBy</c>, and so on) since an actor is always another
/// <see cref="UserAccount"/> instance -- a cross-aggregate-instance reference, per
/// this platform's own standing "reference by identifier, never by instance" rule.
/// domain-events.md's own "Event Principles" states events never carry
/// credentials, tokens, or secrets, and that expiry events carry no actor at all
/// (none acted) -- both followed exactly below.
/// </summary>
public sealed record UserAccountProvisioned(
    Guid EventId, DateTimeOffset OccurredOnUtc, UserAccountId UserAccountId, Guid TenantId, AccountType AccountType,
    Guid? EmployeeId, Guid ProvisionedBy) : IDomainEvent;

public sealed record UserAccountActivated(Guid EventId, DateTimeOffset OccurredOnUtc, UserAccountId UserAccountId) : IDomainEvent;

public sealed record UserAccountSuspended(
    Guid EventId, DateTimeOffset OccurredOnUtc, UserAccountId UserAccountId, Guid SuspendedBy, string Reason) : IDomainEvent;

public sealed record UserAccountReinstated(Guid EventId, DateTimeOffset OccurredOnUtc, UserAccountId UserAccountId) : IDomainEvent;

public sealed record UserAccountDeprovisioned(
    Guid EventId, DateTimeOffset OccurredOnUtc, UserAccountId UserAccountId, Guid DeprovisionedBy, string Reason,
    IReadOnlyList<Guid> RevokedAssignmentIds) : IDomainEvent;

public sealed record RoleGranted(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    UserAccountId UserAccountId,
    RoleAssignmentId RoleAssignmentId,
    string Role,
    string Scope,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    Guid GrantedBy,
    string Reason,
    Guid? ApprovalReference) : IDomainEvent;

public sealed record RoleRevoked(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    UserAccountId UserAccountId,
    RoleAssignmentId RoleAssignmentId,
    string Role,
    string Scope,
    Guid RevokedBy,
    string Reason) : IDomainEvent;

/// <summary>No actor: expiry is automatic, per role-assignments.md's own "Automatic Expiry" section.</summary>
public sealed record RoleAssignmentExpired(
    Guid EventId, DateTimeOffset OccurredOnUtc, UserAccountId UserAccountId, RoleAssignmentId RoleAssignmentId) : IDomainEvent;

public sealed record TenantRoleDefined(Guid EventId, DateTimeOffset OccurredOnUtc, TenantRoleId TenantRoleId, Guid CreatedBy) : IDomainEvent;

/// <summary>
/// Carries the full composed permission set, per domain-events.md: "so that a
/// later review can reconstruct what the role conferred at the time of
/// publication without depending on current configuration."
/// </summary>
public sealed record TenantRolePublished(
    Guid EventId, DateTimeOffset OccurredOnUtc, TenantRoleId TenantRoleId, string Name, IReadOnlyList<string> PermissionSet,
    Guid PublishedBy) : IDomainEvent;

public sealed record TenantRoleDeprecated(Guid EventId, DateTimeOffset OccurredOnUtc, TenantRoleId TenantRoleId) : IDomainEvent;

public sealed record TenantRolePermissionsChanged(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    TenantRoleId TenantRoleId,
    IReadOnlyList<string> AddedPermissions,
    IReadOnlyList<string> RemovedPermissions,
    Guid ChangedBy,
    string Reason) : IDomainEvent;

public sealed record AdministrativeDelegationCreated(
    Guid EventId, DateTimeOffset OccurredOnUtc, DelegationId DelegationId, Guid DelegatorUserAccountId,
    Guid DelegateUserAccountId, string Reason, Guid? ApprovalReference) : IDomainEvent;

public sealed record AdministrativeDelegationActivated(Guid EventId, DateTimeOffset OccurredOnUtc, DelegationId DelegationId) : IDomainEvent;

/// <summary>No actor: expiry is automatic, per delegated-administration.md AR-043.</summary>
public sealed record AdministrativeDelegationExpired(Guid EventId, DateTimeOffset OccurredOnUtc, DelegationId DelegationId) : IDomainEvent;

public sealed record AdministrativeDelegationRevoked(
    Guid EventId, DateTimeOffset OccurredOnUtc, DelegationId DelegationId, Guid RevokedBy, string Reason) : IDomainEvent;
