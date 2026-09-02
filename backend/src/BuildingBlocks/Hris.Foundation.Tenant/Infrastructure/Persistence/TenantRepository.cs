using Hris.Foundation.Tenant.Domain;
using Hris.Infrastructure.Persistence;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;
using TenantAggregate = Hris.Foundation.Tenant.Domain.Tenant;

namespace Hris.Foundation.Tenant.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="ITenantRepository"/>, per repositories.md's
/// "interface in the Domain layer... implementation in Infrastructure" split. No
/// <c>UpdateAsync</c>: an aggregate loaded through <see cref="GetByIdAsync"/> is
/// already tracked by this same <see cref="HrisDbContext"/>, so the caller's own
/// <c>TransactionBehavior</c> persists any mutation via change tracking alone.
/// </summary>
internal sealed class TenantRepository : ITenantRepository
{
    private readonly HrisDbContext _dbContext;

    public TenantRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public Task<TenantAggregate?> GetByIdAsync(TenantId id, CancellationToken cancellationToken) =>
        _dbContext.Set<TenantAggregate>().FirstOrDefaultAsync(tenant => tenant.Id == id, cancellationToken);

    // VERIFIED: TenantCode's own Value Object equality (tenant.TenantCode == tenantCode)
    // compares a HasConversion-mapped property to a constant -- the identical shape
    // HEP-38/HEP-51 already confirmed translates correctly against real PostgreSQL for
    // every other single-column Value Object comparison in this codebase (Key,
    // Country, Username, and the rest) -- see
    // Hris.Infrastructure.IntegrationTests.RepositoryQueryTranslationTests for that
    // precedent's own evidence.
    public Task<TenantAggregate?> GetByTenantCodeAsync(TenantCode tenantCode, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(tenantCode, nameof(tenantCode));

        return _dbContext.Set<TenantAggregate>()
            .FirstOrDefaultAsync(tenant => tenant.TenantCode == tenantCode, cancellationToken);
    }

    public Task<bool> ExistsByTenantCodeAsync(TenantCode tenantCode, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(tenantCode, nameof(tenantCode));

        return _dbContext.Set<TenantAggregate>()
            .AnyAsync(tenant => tenant.TenantCode == tenantCode, cancellationToken);
    }

    public async Task<IReadOnlyCollection<TenantAggregate>> GetAllAsync(CancellationToken cancellationToken) =>
        await _dbContext.Set<TenantAggregate>().ToListAsync(cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyDictionary<TenantLifecycleState, int>> CountByLifecycleStateAsync(CancellationToken cancellationToken)
    {
        var counts = await _dbContext.Set<TenantAggregate>()
            .GroupBy(tenant => tenant.LifecycleState)
            .Select(group => new { State = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return counts.ToDictionary(entry => entry.State, entry => entry.Count);
    }

    public async Task<IReadOnlyDictionary<SubscriptionPlan, int>> CountBySubscriptionPlanAsync(CancellationToken cancellationToken)
    {
        var counts = await _dbContext.Set<TenantAggregate>()
            .GroupBy(tenant => tenant.SubscriptionPlan)
            .Select(group => new { Plan = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return counts.ToDictionary(entry => entry.Plan, entry => entry.Count);
    }

    public async Task AddAsync(TenantAggregate tenant, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(tenant, nameof(tenant));
        await _dbContext.Set<TenantAggregate>().AddAsync(tenant, cancellationToken).ConfigureAwait(false);
    }
}
