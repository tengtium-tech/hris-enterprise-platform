namespace Hris.Foundation.Authorization.Domain;

/// <summary>
/// Persistence abstraction for the <see cref="RolePermissionGrant"/> Aggregate Root,
/// per repositories.md's "interface in the Domain layer... implementation in
/// Infrastructure" split. No Infrastructure implementation exists yet
/// (backend/README.md).
/// </summary>
public interface IRolePermissionGrantRepository
{
    /// <summary>
    /// Added alongside this framework's Application layer, for
    /// <c>RevokePermissionCommand</c> -- the one caller that needs a single grant by
    /// its own identity rather than every active grant for a role.
    /// </summary>
    Task<RolePermissionGrant?> GetByIdAsync(RolePermissionGrantId id, CancellationToken cancellationToken);

    /// <summary>Every active grant for the given roles -- the set <see cref="AuthorizationEvaluator"/> needs for one evaluation.</summary>
    Task<IReadOnlyList<RolePermissionGrant>> GetActiveGrantsForRolesAsync(IReadOnlyCollection<Role> roles, CancellationToken cancellationToken);

    Task AddAsync(RolePermissionGrant grant, CancellationToken cancellationToken);
}
