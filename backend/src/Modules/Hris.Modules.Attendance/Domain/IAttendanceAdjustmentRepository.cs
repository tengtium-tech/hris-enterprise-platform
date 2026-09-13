namespace Hris.Modules.Attendance.Domain;

/// <summary>
/// Repository contract for the <see cref="AttendanceAdjustment"/> Aggregate Root. Source:
/// docs/04-modules/attendance/domain/aggregates.md.
/// </summary>
public interface IAttendanceAdjustmentRepository
{
    Task<AttendanceAdjustment?> GetByIdAsync(AttendanceAdjustmentId id, CancellationToken cancellationToken);

    Task<IReadOnlyList<AttendanceAdjustment>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken);

    Task<IReadOnlyList<AttendanceAdjustment>> ListByRecordAsync(
        AttendanceRecordId recordId, CancellationToken cancellationToken);

    /// <summary>Pending (not yet resolved) adjustment against a record, for AT-031 enforcement.</summary>
    Task<AttendanceAdjustment?> GetPendingForRecordAsync(
        AttendanceRecordId recordId, CancellationToken cancellationToken);

    Task AddAsync(AttendanceAdjustment adjustment, CancellationToken cancellationToken);
}
