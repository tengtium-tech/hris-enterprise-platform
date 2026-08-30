using Hris.SharedKernel;

namespace Hris.Foundation.Authorization.Domain;

/// <summary>
/// Where a <see cref="RoleAssignment"/> reaches, per authorization-framework.md's
/// Delegated Administration section: "Delegation is expressed as role plus scope,
/// never as a new role." <see cref="ScopeId"/> is a raw <see cref="Guid"/>, not a
/// strongly typed <c>TenantId</c>/<c>DepartmentId</c>: which aggregate it identifies
/// is determined entirely by <see cref="Level"/>, and Authorization Framework has no
/// dependency on Tenant or Organization modules, neither of which exists yet
/// (`CTR-ARC-002`).
/// </summary>
public sealed class OrganizationalScope : ValueObject
{
    public OrganizationalScopeLevel Level { get; }

    public Guid ScopeId { get; }

    private OrganizationalScope(OrganizationalScopeLevel level, Guid scopeId)
    {
        Level = level;
        ScopeId = scopeId;
    }

    public static Result<OrganizationalScope> Create(OrganizationalScopeLevel level, Guid scopeId)
    {
        return scopeId == Guid.Empty
            ? Result.Failure<OrganizationalScope>(AuthorizationErrors.ScopeIdRequired)
            : Result.Success(new OrganizationalScope(level, scopeId));
    }

    /// <summary>
    /// Whether a grant at this scope reaches a resource located at
    /// <paramref name="resourceScope"/>: the same level, the same concrete scope id,
    /// per `CTR-AUT-006` ("Scope Is Enforced, Not Only Assigned" -- a role granted at
    /// a restricted scope must not permit access outside it). Deliberately does not
    /// treat a broader <see cref="Level"/> as automatically covering a narrower one
    /// (e.g. a <c>LegalEntity</c>-scoped grant does not, by this method alone, cover
    /// a <c>Department</c> inside it) -- this type has no organizational hierarchy
    /// data to know which department belongs to which legal entity; that
    /// containment check belongs to whichever future module owns the org structure
    /// (`organization`, Phase 2) and must be supplied by the caller, not assumed
    /// here.
    /// </summary>
    public bool Covers(OrganizationalScope resourceScope)
    {
        Guard.AgainstNull(resourceScope, nameof(resourceScope));
        return Level == resourceScope.Level && ScopeId == resourceScope.ScopeId;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Level;
        yield return ScopeId;
    }

    public override string ToString() => $"{Level}:{ScopeId}";
}
