namespace Hris.Modules.Administration.Domain;

/// <summary>
/// Repository contract for the <see cref="TenantRole"/> Aggregate Root.
/// </summary>
public interface ITenantRoleRepository
{
    Task<TenantRole?> GetByIdAsync(TenantRoleId id, CancellationToken cancellationToken);

    Task<IReadOnlyList<TenantRole>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken);

    Task<bool> ExistsWithNameAsync(Guid tenantId, string name, TenantRoleId? excludeId, CancellationToken cancellationToken);

    Task AddAsync(TenantRole role, CancellationToken cancellationToken);

    void Remove(TenantRole role);
}
