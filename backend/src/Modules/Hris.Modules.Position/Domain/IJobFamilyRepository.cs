namespace Hris.Modules.Position.Domain;

/// <summary>
/// Repository contract for the <see cref="JobFamily"/> Aggregate Root. Both
/// <see cref="ExistsWithCodeAsync"/> and <see cref="ExistsWithNameAsync"/> exist
/// because job-families.md states both the code and the name are unique tenant-wide
/// ("Every Job Family has a unique code... a unique name") -- the identical
/// two-uniqueness-check shape <c>IOrganizationRepository</c> already establishes for
/// Organization name/code.
/// </summary>
public interface IJobFamilyRepository
{
    Task<JobFamily?> GetByIdAsync(JobFamilyId id, CancellationToken cancellationToken);

    Task<IReadOnlyList<JobFamily>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken);

    Task<bool> ExistsWithCodeAsync(Guid tenantId, string code, JobFamilyId? excludeId, CancellationToken cancellationToken);

    Task<bool> ExistsWithNameAsync(Guid tenantId, string name, JobFamilyId? excludeId, CancellationToken cancellationToken);

    Task AddAsync(JobFamily jobFamily, CancellationToken cancellationToken);
}
