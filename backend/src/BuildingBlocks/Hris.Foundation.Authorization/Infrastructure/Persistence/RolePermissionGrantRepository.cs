using Hris.Foundation.Authorization.Domain;
using Hris.Infrastructure.Persistence;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hris.Foundation.Authorization.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IRolePermissionGrantRepository"/>.
/// </summary>
/// <remarks>
/// UNVERIFIED (backend/README.md, "What hasn't been verified yet"): <see cref="GetActiveGrantsForRolesAsync"/>'s
/// <c>roles.Contains(grant.Role)</c> translates a client-side collection into a SQL
/// <c>IN</c> clause over a plain enum column -- a simpler translation than the Value
/// Object comparisons <c>ConfigurationSettingRepository</c>'s own remarks flag as
/// unverified, but still not run against a real PostgreSQL instance in this sandbox.
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
