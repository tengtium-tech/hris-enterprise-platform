using Hris.SharedKernel;

namespace Hris.Foundation.Authorization.Domain;

/// <summary>
/// Records that <see cref="Role"/> carries <see cref="Permission"/>, per
/// authorization-framework.md's "Permission Management" capability and its own
/// Centralized Evaluation section: "Roles are collections of permissions." A
/// separate Aggregate Root from <see cref="RoleAssignment"/> -- this grants a
/// permission *to a role*, independent of which principals currently hold that role,
/// while <see cref="RoleAssignment"/> grants a role *to a principal* -- so revoking
/// or adding a role's permission does not require touching every principal's own
/// assignment.
///
/// No module exists yet to register a concrete permission set (Phase 2 onward) --
/// this Aggregate is the mechanism a future module's own Infrastructure seeding will
/// populate, not a pre-populated catalog invented here ahead of any resource that
/// needs it.
/// </summary>
public sealed class RolePermissionGrant : AggregateRoot<RolePermissionGrantId>
{
    /// <summary>
    /// The seven <see cref="PermissionAction"/> values `CTR-AUT-003` ("Auditor Holds
    /// No Mutation Permissions") treats as a mutation, per that requirement's own
    /// text: "The Auditor role must not be able to create, modify, or delete any
    /// business record." Read, Export, and Execute are deliberately not included --
    /// none of them, on their own, creates, modifies, or deletes a business record.
    /// </summary>
    private static readonly HashSet<PermissionAction> _mutatingActions =
    [
        PermissionAction.Create,
        PermissionAction.Update,
        PermissionAction.Delete,
        PermissionAction.Approve,
        PermissionAction.Reject,
        PermissionAction.Import,
        PermissionAction.Configure,
    ];

    public Role Role { get; }

    public PermissionKey Permission { get; }

    public DateTimeOffset GrantedAtUtc { get; }

    public DateTimeOffset? RevokedAtUtc { get; private set; }

    private RolePermissionGrant(RolePermissionGrantId id, Role role, PermissionKey permission, DateTimeOffset grantedAtUtc)
        : base(id)
    {
        Role = role;
        Permission = permission;
        GrantedAtUtc = grantedAtUtc;
    }

    /// <summary>
    /// Structurally enforces `CTR-AUT-003`: a grant pairing <see cref="Role.Auditor"/>
    /// with a mutating <see cref="PermissionAction"/> is rejected at the one point
    /// every such grant must pass through, rather than relying on every future
    /// caller to remember the rule (this project's own engineering principle: "Prefer
    /// structure over discipline").
    /// </summary>
    public static Result<RolePermissionGrant> Create(Role role, PermissionKey permission, DateTimeOffset nowUtc)
    {
        Guard.AgainstNull(permission, nameof(permission));

        if (role == Role.Auditor && _mutatingActions.Contains(permission.Action))
        {
            return Result.Failure<RolePermissionGrant>(AuthorizationErrors.AuditorCannotHoldMutationPermission);
        }

        var grant = new RolePermissionGrant(new RolePermissionGrantId(Guid.NewGuid()), role, permission, nowUtc);
        grant.AddDomainEvent(new PermissionGranted(Guid.NewGuid(), nowUtc, grant.Id, role, permission));
        return Result.Success(grant);
    }

    public bool IsActive => RevokedAtUtc is null;

    public Result Revoke(DateTimeOffset nowUtc)
    {
        if (RevokedAtUtc is not null)
        {
            return Result.Success();
        }

        RevokedAtUtc = nowUtc;
        AddDomainEvent(new PermissionRevoked(Guid.NewGuid(), nowUtc, Id, Role, Permission));
        return Result.Success();
    }
}
