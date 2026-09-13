namespace Hris.Modules.Attendance.Domain;

/// <summary>
/// Repository contract for the <see cref="AttendancePolicy"/> Aggregate Root. There is
/// deliberately no repository for <see cref="PolicyAssignment"/> — it is reached through
/// its owning policy (CTR-ARC-004). Source: docs/04-modules/attendance/domain/aggregates.md.
/// </summary>
public interface IAttendancePolicyRepository
{
    Task<AttendancePolicy?> GetByIdAsync(AttendancePolicyId id, CancellationToken cancellationToken);

    Task<IReadOnlyList<AttendancePolicy>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken);

    Task<IReadOnlyList<AttendancePolicy>> ListByLineageAsync(Guid lineageId, CancellationToken cancellationToken);

    /// <summary>Active versions effective on a work date (AT-002), from which the effective one is resolved.</summary>
    Task<IReadOnlyList<AttendancePolicy>> ListEffectiveForWorkDateAsync(
        Guid tenantId, DateOnly workDate, CancellationToken cancellationToken);

    Task AddAsync(AttendancePolicy policy, CancellationToken cancellationToken);
}
