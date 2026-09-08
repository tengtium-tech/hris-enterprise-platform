namespace Hris.Modules.Administration.Domain;

/// <summary>
/// Repository contract for the <see cref="AdministrativeDelegation"/> Aggregate Root.
/// </summary>
public interface IAdministrativeDelegationRepository
{
    Task<AdministrativeDelegation?> GetByIdAsync(DelegationId id, CancellationToken cancellationToken);

    Task<IReadOnlyList<AdministrativeDelegation>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken);

    Task AddAsync(AdministrativeDelegation delegation, CancellationToken cancellationToken);
}
