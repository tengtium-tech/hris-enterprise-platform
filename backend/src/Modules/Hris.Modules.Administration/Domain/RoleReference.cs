using Hris.SharedKernel;

namespace Hris.Modules.Administration.Domain;

/// <summary>
/// Identifies a role, canonical or tenant-defined. Source:
/// docs/04-modules/administration/domain/value-objects.md's RoleReference.
/// <see cref="TenantRoleId"/> is a plain <see cref="Guid"/> even though it
/// references this same module's own <see cref="Domain.TenantRole"/> Aggregate
/// Root, per this platform's own standing "Aggregates communicate through
/// identifiers, never a direct instance reference" rule, the identical reasoning
/// already applied to Employee module's own <c>EmployeeHistory.EmployeeId</c>.
/// <see cref="TenantRoleName"/> is denormalized onto the reference so a
/// <see cref="RoleAssignment"/> remains readable (and, per
/// domain-events.md's <c>TenantRolePublished</c>, historically reconstructable)
/// without a join back to a <see cref="Domain.TenantRole"/> that may since have
/// changed name or been deprecated.
/// </summary>
public sealed class RoleReference : ValueObject
{
    public RoleKind Kind { get; }

    public CanonicalRole? CanonicalRole { get; }

    public Guid? TenantRoleId { get; }

    public string? TenantRoleName { get; }

    private RoleReference(RoleKind kind, CanonicalRole? canonicalRole, Guid? tenantRoleId, string? tenantRoleName)
    {
        Kind = kind;
        CanonicalRole = canonicalRole;
        TenantRoleId = tenantRoleId;
        TenantRoleName = tenantRoleName;
    }

    public static RoleReference ForCanonical(CanonicalRole canonicalRole) =>
        new(RoleKind.Canonical, canonicalRole, null, null);

    /// <summary>
    /// <paramref name="isPublished"/> is computed by the calling Application-layer
    /// command handler against <c>ITenantRoleRepository</c> before this method is
    /// called, enforcing "a Tenant reference must resolve to a Published tenant
    /// role" -- this Aggregate never reaches into a <see cref="Domain.TenantRole"/>
    /// instance directly.
    /// </summary>
    public static Result<RoleReference> ForTenantRole(Guid tenantRoleId, string? tenantRoleName, bool isPublished)
    {
        if (string.IsNullOrWhiteSpace(tenantRoleName) || !isPublished)
        {
            return Result.Failure<RoleReference>(AdministrationErrors.TenantRoleReferenceRequiresPublishedRole);
        }

        return Result.Success(new RoleReference(RoleKind.Tenant, null, tenantRoleId, tenantRoleName.Trim()));
    }

    public string DisplayName => Kind == RoleKind.Canonical ? CanonicalRole!.Value.ToString() : TenantRoleName!;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Kind;
        yield return CanonicalRole;
        yield return TenantRoleId;
    }

    public override string ToString() => DisplayName;
}
