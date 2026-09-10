namespace Hris.Modules.Timekeeping.Domain;

/// <summary>Repository contract for the <see cref="WorkShift"/> Aggregate Root.</summary>
public interface IWorkShiftRepository
{
    Task<WorkShift?> GetByIdAsync(WorkShiftId id, CancellationToken cancellationToken);

    Task<IReadOnlyList<WorkShift>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken);

    Task<IReadOnlyList<WorkShift>> ListByLineageAsync(Guid lineageId, CancellationToken cancellationToken);

    /// <summary>Supports the tenant-uniqueness check a shift code carries.</summary>
    Task<bool> CodeExistsInTenantAsync(
        Guid tenantId, string code, WorkShiftId? excluding, CancellationToken cancellationToken);

    Task AddAsync(WorkShift shift, CancellationToken cancellationToken);
}
