namespace Hris.Modules.Organization.Domain;

/// <summary>
/// Repository contract for the <see cref="WorkLocation"/> Aggregate Root. LOC-001's
/// tenant-wide code uniqueness is checked via <see cref="ExistsWithCodeAsync"/> by
/// the Application layer before <see cref="WorkLocation.Create"/> is called, the
/// identical split <see cref="IOrganizationRepository"/> already establishes for
/// ORG-001/ORG-002.
/// </summary>
public interface IWorkLocationRepository
{
    Task<WorkLocation?> GetByIdAsync(WorkLocationId id, CancellationToken cancellationToken);

    Task<IReadOnlyList<WorkLocation>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken);

    Task<bool> ExistsWithCodeAsync(Guid tenantId, string code, WorkLocationId? excludeId, CancellationToken cancellationToken);

    Task AddAsync(WorkLocation workLocation, CancellationToken cancellationToken);
}
