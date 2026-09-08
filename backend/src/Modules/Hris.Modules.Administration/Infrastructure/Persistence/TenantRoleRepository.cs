using Hris.Infrastructure.Persistence;
using Hris.Modules.Administration.Domain;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hris.Modules.Administration.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="ITenantRoleRepository"/>.
/// </summary>
internal sealed class TenantRoleRepository : ITenantRoleRepository
{
    private readonly HrisDbContext _dbContext;

    public TenantRoleRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public Task<TenantRole?> GetByIdAsync(TenantRoleId id, CancellationToken cancellationToken) =>
        _dbContext.Set<TenantRole>().FirstOrDefaultAsync(role => role.Id == id, cancellationToken);

    public async Task<IReadOnlyList<TenantRole>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return await _dbContext.Set<TenantRole>()
            .Where(role => role.TenantId == tenantId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> ExistsWithNameAsync(Guid tenantId, string name, TenantRoleId? excludeId, CancellationToken cancellationToken)
    {
        var normalized = name.Trim();
        return await _dbContext.Set<TenantRole>()
            .AnyAsync(
                role => role.TenantId == tenantId && role.Name == normalized && (excludeId == null || role.Id != excludeId),
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(TenantRole role, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(role, nameof(role));
        await _dbContext.Set<TenantRole>().AddAsync(role, cancellationToken).ConfigureAwait(false);
    }

    public void Remove(TenantRole role)
    {
        Guard.AgainstNull(role, nameof(role));
        _dbContext.Set<TenantRole>().Remove(role);
    }
}
