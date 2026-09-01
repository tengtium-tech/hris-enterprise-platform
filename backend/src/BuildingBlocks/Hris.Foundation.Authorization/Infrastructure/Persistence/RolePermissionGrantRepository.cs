using Hris.Foundation.Authorization.Domain;
using Hris.Infrastructure.Persistence;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hris.Foundation.Authorization.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IRolePermissionGrantRepository"/>.
/// </summary>
/// <remarks>
/// VERIFIED: <see cref="GetActiveGrantsForRolesAsync"/>'s
/// <c>roles.Contains(grant.Role)</c> translates a client-side collection into a SQL
/// <c>IN</c> clause over a plain enum column -- a simpler translation than the Value
/// Object comparisons this Sprint's own HEP-38 work verified, but confirmed rather
/// than skipped as "obviously fine": run against a real, disposable PostgreSQL 16
/// instance via Testcontainers -- see
/// <c>Hris.Infrastructure.IntegrationTests.RepositoryQueryTranslationTests.RolePermissionGrantRepository_GetActiveGrantsForRolesAsync_TranslatesRolesContains</c>.
/// Passes: no fix needed.
/// </remarks>
internal sealed class RolePermissionGrantRepository : IRolePermissionGrantRepository
{
    private readonly HrisDbContext _dbContext;

    public RolePermissionGrantRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public Task<RolePermissionGrant?> GetByIdAsync(RolePermissionGrantId id, CancellationToken cancellationToken) =>
        _dbContext.Set<RolePermissionGrant>()
            .FirstOrDefaultAsync(grant => grant.Id == id, cancellationToken);

    public async Task<IReadOnlyList<RolePermissionGrant>> GetActiveGrantsForRolesAsync(
        IReadOnlyCollection<Role> roles, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(roles, nameof(roles));

        return await _dbContext.Set<RolePermissionGrant>()
            .Where(grant => roles.Contains(grant.Role) && grant.RevokedAtUtc == null)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(RolePermissionGrant grant, CancellationToken cancellationToken) =>
        await _dbContext.Set<RolePermissionGrant>()
            .AddAsync(grant, cancellationToken)
            .ConfigureAwait(false);
}
