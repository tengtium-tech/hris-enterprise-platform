namespace Hris.Modules.Attendance.Domain;

/// <summary>
/// Repository contract for the <see cref="AttendanceRecord"/> Aggregate Root. There is
/// deliberately no repository for <see cref="TimeEvent"/> — it is reached through its
/// owning record (CTR-ARC-004). Source: docs/04-modules/attendance/domain/aggregates.md.
/// </summary>
public interface IAttendanceRecordRepository
{
    Task<AttendanceRecord?> GetByIdAsync(AttendanceRecordId id, CancellationToken cancellationToken);

    Task<IReadOnlyList<AttendanceRecord>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken);

    /// <summary>Returns the single record for an employee on a work date, or null (AT-012).</summary>
    Task<AttendanceRecord?> GetByEmployeeAndWorkDateAsync(
        Guid tenantId, Guid employeeId, DateOnly workDate, CancellationToken cancellationToken);

    /// <summary>
    /// Payroll's dependency: every finalized record for a payroll period and scope,
    /// complete and stable (queries.md). Excludes nothing here — pending-adjustment
    /// exclusion is enforced at the query layer against the adjustment store.
    /// </summary>
    Task<IReadOnlyList<AttendanceRecord>> GetFinalizedForPayrollPeriodAsync(
        Guid tenantId, DateOnly periodStart, DateOnly periodEnd, CancellationToken cancellationToken);

    Task AddAsync(AttendanceRecord record, CancellationToken cancellationToken);
}
