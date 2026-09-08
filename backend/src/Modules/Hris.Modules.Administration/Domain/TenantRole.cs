using Hris.SharedKernel;

namespace Hris.Modules.Administration.Domain;

/// <summary>
/// Aggregate Root representing a tenant-defined role composed additively from
/// existing platform permissions. Source:
/// docs/04-modules/administration/domain/aggregates.md and entities.md.
///
/// ADR-0002 records that roles and permissions are configuration rather than
/// code, which permits tenant-specific role definitions -- this is where a tenant
/// composes one. A tenant role is additive only (AR-030, AR-031): it may not use
/// or redefine a canonical role name, and it may compose only permissions that
/// already exist in the platform permission model.
/// </summary>
public sealed class TenantRole : AggregateRoot<TenantRoleId>
{
    private readonly List<PermissionGrant> _permissionGrants = [];

    public Guid TenantId { get; }

    public string Name { get; }

    public string? Description { get; }

    public TenantRoleStatus Status { get; private set; }

    public Guid CreatedBy { get; }

    public DateTimeOffset CreatedOn { get; }

    public Guid? PublishedBy { get; private set; }

    public DateTimeOffset? PublishedOn { get; private set; }

    public IReadOnlyList<PermissionGrant> PermissionGrants => _permissionGrants.AsReadOnly();

    private TenantRole(TenantRoleId id, Guid tenantId, string name, string? description, Guid createdBy, DateTimeOffset createdOn)
        : base(id)
    {
        TenantId = tenantId;
        Name = name;
        Description = description;
        Status = TenantRoleStatus.Draft;
        CreatedBy = createdBy;
        CreatedOn = createdOn;
    }

    /// <summary>
    /// <paramref name="nameCollidesWithCanonicalRole"/> is computed by comparing
    /// <paramref name="name"/> against <see cref="CanonicalRole"/>'s own closed
    /// enumeration (AR-030) -- a same-module, no-cross-reference check, unlike
    /// <paramref name="nameAlreadyExistsInTenant"/>, which the calling
    /// Application-layer command handler computes via
    /// <c>ITenantRoleRepository.ExistsWithNameAsync</c>.
    /// </summary>
    public static Result<TenantRole> Create(
        TenantRoleId id, Guid tenantId, string? name, string? description, bool nameCollidesWithCanonicalRole,
        bool nameAlreadyExistsInTenant, Guid createdBy, DateTimeOffset nowUtc)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<TenantRole>(AdministrationErrors.RoleNameRequired);
        }

        var normalizedName = name.Trim();
        if (normalizedName.Length > 100)
        {
            return Result.Failure<TenantRole>(AdministrationErrors.RoleNameTooLong);
        }

        if (nameCollidesWithCanonicalRole || nameAlreadyExistsInTenant)
        {
            return Result.Failure<TenantRole>(AdministrationErrors.RoleNameCollidesWithCanonicalRole);
        }

        var role = new TenantRole(id, tenantId, normalizedName, description?.Trim(), createdBy, nowUtc);
        role.AddDomainEvent(new TenantRoleDefined(Guid.NewGuid(), nowUtc, id, createdBy));
        return Result.Success(role);
    }

    /// <summary>AddPermissionToTenantRoleCommand's own aggregate method -- Draft only, no reason required.</summary>
    public Result AddPermission(string? permission, Guid addedBy, DateTimeOffset nowUtc)
    {
        if (Status != TenantRoleStatus.Draft)
        {
            return Result.Failure(AdministrationErrors.TenantRoleNotDraft);
        }

        var permissionResult = PermissionReference.Create(permission);
        if (permissionResult.IsFailure)
        {
            return Result.Failure(permissionResult.Error);
        }

        if (_permissionGrants.Any(g => g.Permission == permissionResult.Value))
        {
            return Result.Failure(AdministrationErrors.DuplicatePermissionGrant);
        }

        _permissionGrants.Add(new PermissionGrant(new PermissionGrantId(Guid.NewGuid()), permissionResult.Value, addedBy, nowUtc));
        return Result.Success();
    }

    public Result RemovePermission(Guid permissionGrantId)
    {
        if (Status != TenantRoleStatus.Draft)
        {
            return Result.Failure(AdministrationErrors.TenantRoleNotDraft);
        }

        var grant = _permissionGrants.SingleOrDefault(g => g.Id.Value == permissionGrantId);
        if (grant is null)
        {
            return Result.Failure(AdministrationErrors.PermissionGrantNotFound);
        }

        _permissionGrants.Remove(grant);
        return Result.Success();
    }

    /// <summary>
    /// Carries the full composed permission set on <see cref="TenantRolePublished"/>
    /// so a later review can reconstruct what the role conferred at the time of
    /// publication (domain-events.md).
    /// </summary>
    public Result Publish(Guid publishedBy, DateTimeOffset nowUtc)
    {
        if (Status != TenantRoleStatus.Draft)
        {
            return Result.Failure(AdministrationErrors.TenantRoleNotDraft);
        }

        Status = TenantRoleStatus.Published;
        PublishedBy = publishedBy;
        PublishedOn = nowUtc;

        AddDomainEvent(new TenantRolePublished(
            Guid.NewGuid(), nowUtc, Id, Name, _permissionGrants.Select(g => g.Permission.Value).ToList(), publishedBy));
        return Result.Success();
    }

    /// <summary>
    /// ChangeTenantRolePermissionsCommand's own aggregate method -- Published
    /// only, requires a reason (unlike Draft composition), and silently changes
    /// the authority of every user holding the role (domain-events.md's own
    /// warning on <see cref="TenantRolePermissionsChanged"/>).
    /// </summary>
    public Result ChangePermissions(
        IReadOnlyList<string?> permissionsToAdd, IReadOnlyList<Guid> permissionGrantIdsToRemove, Guid changedBy, string? reason,
        DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(permissionsToAdd);
        ArgumentNullException.ThrowIfNull(permissionGrantIdsToRemove);

        if (Status != TenantRoleStatus.Published)
        {
            return Result.Failure(AdministrationErrors.TenantRoleNotPublished);
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result.Failure(AdministrationErrors.GrantReasonRequired);
        }

        var addedNames = new List<string>();
        foreach (var permission in permissionsToAdd)
        {
            var permissionResult = PermissionReference.Create(permission);
            if (permissionResult.IsFailure)
            {
                return Result.Failure(permissionResult.Error);
            }

            if (_permissionGrants.Any(g => g.Permission == permissionResult.Value))
            {
                continue;
            }

            _permissionGrants.Add(new PermissionGrant(new PermissionGrantId(Guid.NewGuid()), permissionResult.Value, changedBy, nowUtc));
            addedNames.Add(permissionResult.Value.Value);
        }

        var removedNames = new List<string>();
        foreach (var grantId in permissionGrantIdsToRemove)
        {
            var grant = _permissionGrants.SingleOrDefault(g => g.Id.Value == grantId);
            if (grant is null)
            {
                continue;
            }

            _permissionGrants.Remove(grant);
            removedNames.Add(grant.Permission.Value);
        }

        AddDomainEvent(new TenantRolePermissionsChanged(Guid.NewGuid(), nowUtc, Id, addedNames, removedNames, changedBy, reason.Trim()));
        return Result.Success();
    }

    public Result Deprecate(DateTimeOffset nowUtc)
    {
        if (Status != TenantRoleStatus.Published)
        {
            return Result.Failure(AdministrationErrors.TenantRoleNotPublished);
        }

        Status = TenantRoleStatus.Deprecated;
        AddDomainEvent(new TenantRoleDeprecated(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    /// <summary>
    /// AR-032: a role referenced by any active assignment cannot be deleted.
    /// <paramref name="isReferencedByActiveAssignment"/> is computed by the
    /// calling Application-layer command handler via
    /// <c>IUserAccountRepository</c>, a cross-aggregate fact this Aggregate cannot
    /// determine about itself. Deletion itself is a repository-level removal
    /// performed by the caller once this guard passes -- there is no in-aggregate
    /// state transition to a "deleted" status.
    /// </summary>
#pragma warning disable CA1822 // Instance method by design: reads as "this role's" deletability at call sites (role.EnsureDeletable(...)), even though the check itself is the caller-supplied boolean.
    public Result EnsureDeletable(bool isReferencedByActiveAssignment) =>
#pragma warning restore CA1822
        isReferencedByActiveAssignment
            ? Result.Failure(AdministrationErrors.TenantRoleInUseCannotBeDeleted)
            : Result.Success();
}
