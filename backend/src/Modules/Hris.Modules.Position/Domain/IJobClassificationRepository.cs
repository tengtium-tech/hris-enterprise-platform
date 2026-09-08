namespace Hris.Modules.Position.Domain;

/// <summary>
/// Repository contract for the <see cref="JobClassification"/> Aggregate Root. See
/// <see cref="IJobFamilyRepository"/>'s own remarks for why both code and name
/// uniqueness are checked here.
/// </summary>
public interface IJobClassificationRepository
{
    Task<JobClassification?> GetByIdAsync(JobClassificationId id, CancellationToken cancellationToken);

    Task<IReadOnlyList<JobClassification>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken);

    Task<bool> ExistsWithCodeAsync(
        Guid tenantId, string code, JobClassificationId? excludeId, CancellationToken cancellationToken);

    Task<bool> ExistsWithNameAsync(
        Guid tenantId, string name, JobClassificationId? excludeId, CancellationToken cancellationToken);

    Task AddAsync(JobClassification jobClassification, CancellationToken cancellationToken);
}
