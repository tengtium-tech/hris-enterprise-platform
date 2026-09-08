namespace Hris.Modules.Position.Domain;

/// <summary>
/// Repository contract for the <see cref="JobGrade"/> Aggregate Root. See
/// <see cref="IJobFamilyRepository"/>'s own remarks for why both code and name
/// uniqueness are checked here.
/// </summary>
public interface IJobGradeRepository
{
    Task<JobGrade?> GetByIdAsync(JobGradeId id, CancellationToken cancellationToken);

    Task<IReadOnlyList<JobGrade>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken);

    Task<bool> ExistsWithCodeAsync(Guid tenantId, string code, JobGradeId? excludeId, CancellationToken cancellationToken);

    Task<bool> ExistsWithNameAsync(Guid tenantId, string name, JobGradeId? excludeId, CancellationToken cancellationToken);

    Task AddAsync(JobGrade jobGrade, CancellationToken cancellationToken);
}
