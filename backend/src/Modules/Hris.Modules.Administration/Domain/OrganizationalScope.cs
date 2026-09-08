using Hris.SharedKernel;

namespace Hris.Modules.Administration.Domain;

/// <summary>
/// Expresses where a role applies. Source:
/// docs/04-modules/administration/domain/role-assignments.md's "Scope Levels"
/// table. <see cref="TargetId"/> is a plain <see cref="Guid"/> reference into the
/// `organization` module (or, for a future employee-group scope, elsewhere) --
/// this module never takes a compile-time reference to resolve it. Whether the
/// target actually exists and is of the correct kind (AR-024) is validated by the
/// caller against the `organization` module's own public contract, not here.
/// </summary>
public sealed class OrganizationalScope : ValueObject
{
    public ScopeLevel Level { get; }

    public Guid? TargetId { get; }

    private OrganizationalScope(ScopeLevel level, Guid? targetId)
    {
        Level = level;
        TargetId = targetId;
    }

    public static Result<OrganizationalScope> Create(ScopeLevel level, Guid? targetId)
    {
        var requiresTarget = level is ScopeLevel.LegalEntity or ScopeLevel.BusinessUnit or ScopeLevel.Department or ScopeLevel.Team;

        if (requiresTarget && targetId is null)
        {
            return Result.Failure<OrganizationalScope>(AdministrationErrors.ScopeTargetRequired);
        }

        if (!requiresTarget && targetId is not null)
        {
            return Result.Failure<OrganizationalScope>(AdministrationErrors.ScopeTargetProhibited);
        }

        return Result.Success(new OrganizationalScope(level, targetId));
    }

    /// <summary>
    /// AR-002's "no broader a scope than held" comparison. Broader means a lower
    /// <see cref="ScopeLevel"/> ordinal (<see cref="ScopeLevel.Tenant"/> is
    /// broadest). Two scopes at the same level but different targets are neither
    /// broader nor narrower than each other -- they cover different, unrelated
    /// organizational units, not a subset relationship this comparison can express;
    /// the caller-supplied authority check that uses this method is responsible for
    /// also confirming the target itself matches or descends appropriately, via the
    /// `organization` module's own public contract.
    /// </summary>
    public bool IsBroaderThanOrEqualTo(OrganizationalScope other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return Level <= other.Level;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Level;
        yield return TargetId;
    }

    public override string ToString() => TargetId is null ? Level.ToString() : $"{Level}:{TargetId}";
}
