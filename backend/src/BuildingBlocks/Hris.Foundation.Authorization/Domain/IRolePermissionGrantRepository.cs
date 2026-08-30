namespace Hris.Foundation.Authorization.Domain;

/// <summary>
/// Persistence abstraction for the <see cref="RolePermissionGrant"/> Aggregate Root,
/// per repositories.md's "interface in the Domain layer... implementation in
/// Infrastructure" split. No Infrastructure implementation exists yet
/// (backend/README.md).
/// </summary>
public interface IRolePermissionGrantRepository
{
    /// <summary>Every active grant for the given roles -- the set <see cref="AuthorizationEvaluator"/> needs for one evaluation.</summary>
    Task<IReadOnlyList<RolePermissionGrant>> GetActiveGrantsForRolesAsync(IReadOnlyCollection<Role> roles, CancellationToken cancellationToken);

    Task AddAsync(RolePermissionGrant grant, CancellationToken cancellationToken);
}
