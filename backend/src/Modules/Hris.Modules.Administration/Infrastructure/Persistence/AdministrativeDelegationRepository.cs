using Hris.Infrastructure.Persistence;
using Hris.Modules.Administration.Domain;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hris.Modules.Administration.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IAdministrativeDelegationRepository"/>.
/// </summary>
internal sealed class AdministrativeDelegationRepository : IAdministrativeDelegationRepository
{
    private readonly HrisDbContext _dbContext;

    public AdministrativeDelegationRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public Task<AdministrativeDelegation?> GetByIdAsync(DelegationId id, CancellationToken cancellationToken) =>
        _dbContext.Set<AdministrativeDelegation>().FirstOrDefaultAsync(delegation => delegation.Id == id, cancellationToken);

    public async Task<IReadOnlyList<AdministrativeDelegation>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return await _dbContext.Set<AdministrativeDelegation>()
            .Where(delegation => delegation.TenantId == tenantId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(AdministrativeDelegation delegation, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(delegation, nameof(delegation));
        await _dbContext.Set<AdministrativeDelegation>().AddAsync(delegation, cancellationToken).ConfigureAwait(false);
    }
}
