namespace Hris.Modules.Attendance.Domain;

/// <summary>
/// Repository contract for the <see cref="OvertimeRequest"/> Aggregate Root. Source:
/// docs/04-modules/attendance/domain/aggregates.md.
/// </summary>
public interface IOvertimeRequestRepository
{
    Task<OvertimeRequest?> GetByIdAsync(OvertimeRequestId id, CancellationToken cancellationToken);

    Task<IReadOnlyList<OvertimeRequest>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken);

    Task<OvertimeRequest?> GetByEmployeeAndWorkDateAsync(
        Guid tenantId, Guid employeeId, DateOnly workDate, CancellationToken cancellationToken);

    /// <summary>Approved requests for a work date, which the calculation engine consults (AT-040).</summary>
    Task<IReadOnlyList<OvertimeRequest>> ListApprovedForWorkDateAsync(
        Guid tenantId, DateOnly workDate, CancellationToken cancellationToken);

    Task AddAsync(OvertimeRequest request, CancellationToken cancellationToken);
}
