using Hris.Foundation.Integration.Domain;
using Hris.Infrastructure.Persistence;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hris.Foundation.Integration.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IIntegrationRunRepository"/>, per
/// repositories.md's "interface in the Domain layer... implementation in
/// Infrastructure" split.
/// </summary>
internal sealed class IntegrationRunRepository : IIntegrationRunRepository
{
    private readonly HrisDbContext _dbContext;

    public IntegrationRunRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public Task<IntegrationRun?> GetByIdAsync(IntegrationRunId id, CancellationToken cancellationToken) =>
        _dbContext.Set<IntegrationRun>().FirstOrDefaultAsync(run => run.Id == id, cancellationToken);

    public async Task<IReadOnlyList<IntegrationRun>> ListHistoryAsync(
        Guid tenantId, ConnectorId connectorId, int maxResults, CancellationToken cancellationToken)
    {
        return await _dbContext.Set<IntegrationRun>()
            .Where(run => run.TenantId == tenantId && run.ConnectorId == connectorId)
            .OrderByDescending(run => run.StartedAtUtc)
            .Take(maxResults)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(IntegrationRun integrationRun, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(integrationRun, nameof(integrationRun));
        await _dbContext.Set<IntegrationRun>().AddAsync(integrationRun, cancellationToken).ConfigureAwait(false);
    }
}
