namespace Hris.Foundation.Tenant.Domain;

/// <summary>
/// Persistence abstraction for the <see cref="Tenant"/> Aggregate Root, per
/// repositories.md's "interface in the Domain layer... implementation in
/// Infrastructure" split.
///
/// <see cref="CountByLifecycleStateAsync"/> and <see cref="CountBySubscriptionPlanAsync"/>
/// back <c>GetPlatformDashboardSummaryQuery</c>'s own two per-tenant-registry
/// aggregate counts (tenant-framework.md, Platform-Operator-Facing Commands and
/// Queries) -- grouped in the database rather than loaded into memory via
/// <see cref="GetAllAsync"/> and counted in the handler, since NFR-SC (Scalability,
/// tenant-framework.md's own Non-Functional Requirements) states "support thousands
/// of active tenants," a size where an in-memory group-by is wasteful for a query
/// that only ever needs the counts.
/// </summary>
public interface ITenantRepository
{
    Task<Tenant?> GetByIdAsync(TenantId id, CancellationToken cancellationToken);

    Task<Tenant?> GetByTenantCodeAsync(TenantCode tenantCode, CancellationToken cancellationToken);

    Task<bool> ExistsByTenantCodeAsync(TenantCode tenantCode, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Tenant>> GetAllAsync(CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<TenantLifecycleState, int>> CountByLifecycleStateAsync(CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<SubscriptionPlan, int>> CountBySubscriptionPlanAsync(CancellationToken cancellationToken);

    Task AddAsync(Tenant tenant, CancellationToken cancellationToken);
}
