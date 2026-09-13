namespace Hris.Modules.Attendance.Domain;

/// <summary>
/// Repository contract for the <see cref="AttendanceDevice"/> Aggregate Root. Source:
/// docs/04-modules/attendance/domain/aggregates.md.
/// </summary>
public interface IAttendanceDeviceRepository
{
    Task<AttendanceDevice?> GetByIdAsync(AttendanceDeviceId id, CancellationToken cancellationToken);

    Task<IReadOnlyList<AttendanceDevice>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken);

    Task<AttendanceDevice?> GetBySerialNumberAsync(string serialNumber, CancellationToken cancellationToken);

    Task<IReadOnlyList<AttendanceDevice>> ListActiveAsync(Guid tenantId, CancellationToken cancellationToken);

    Task AddAsync(AttendanceDevice device, CancellationToken cancellationToken);
}
