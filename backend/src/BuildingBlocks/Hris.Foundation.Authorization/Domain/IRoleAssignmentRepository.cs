using Hris.Foundation.Identity.Domain;

namespace Hris.Foundation.Authorization.Domain;

/// <summary>
/// Persistence abstraction for the <see cref="RoleAssignment"/> Aggregate Root, per
/// repositories.md's "interface in the Domain layer... implementation in
/// Infrastructure" split. No Infrastructure implementation exists yet
/// (backend/README.md).
/// </summary>
public interface IRoleAssignmentRepository
{
    Task<RoleAssignment?> GetByIdAsync(RoleAssignmentId id, CancellationToken cancellationToken);

    /// <summary>
    /// Every assignment for a principal -- effective, expired, or revoked.
    /// <see cref="AuthorizationEvaluator"/> filters to what is actually effective
    /// itself, per `CTR-AUT-007`'s own requirement that revocation be re-checked on
    /// every evaluation rather than assumed from a cached or pre-filtered read.
    /// </summary>
    Task<IReadOnlyList<RoleAssignment>> GetByPrincipalAsync(UserAccountId principalId, CancellationToken cancellationToken);

    Task AddAsync(RoleAssignment assignment, CancellationToken cancellationToken);
}
