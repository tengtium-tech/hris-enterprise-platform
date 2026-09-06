using Hris.Foundation.Integration.Domain;
using Hris.Infrastructure.Persistence;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hris.Foundation.Integration.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IConnectorRepository"/>, per repositories.md's
/// "interface in the Domain layer... implementation in Infrastructure" split. No
/// <c>UpdateAsync</c>: an aggregate loaded through <see cref="GetByIdAsync"/> is
/// already tracked by this same <see cref="HrisDbContext"/>, so the caller's own
/// <c>TransactionBehavior</c> persists any mutation via change tracking alone.
/// </summary>
internal sealed class ConnectorRepository : IConnectorRepository
{
    private readonly HrisDbContext _dbContext;

    public ConnectorRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public Task<Connector?> GetByIdAsync(ConnectorId id, CancellationToken cancellationToken) =>
        _dbContext.Set<Connector>().FirstOrDefaultAsync(connector => connector.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Connector>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return await _dbContext.Set<Connector>()
            .Where(connector => connector.TenantId == tenantId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(Connector connector, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(connector, nameof(connector));
        await _dbContext.Set<Connector>().AddAsync(connector, cancellationToken).ConfigureAwait(false);
    }
}
