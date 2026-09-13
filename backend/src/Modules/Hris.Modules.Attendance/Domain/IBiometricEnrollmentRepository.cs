namespace Hris.Modules.Attendance.Domain;

/// <summary>
/// Repository contract for the <see cref="BiometricEnrollment"/> Aggregate Root. Source:
/// docs/04-modules/attendance/domain/aggregates.md.
/// </summary>
public interface IBiometricEnrollmentRepository
{
    Task<BiometricEnrollment?> GetByIdAsync(BiometricEnrollmentId id, CancellationToken cancellationToken);

    Task<IReadOnlyList<BiometricEnrollment>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken);

    Task<BiometricEnrollment?> GetByEmployeeAndMethodAsync(
        Guid tenantId, Guid employeeId, BiometricMethod method, CancellationToken cancellationToken);

    /// <summary>Active enrollments for an employee, which authentication consults (AT-052).</summary>
    Task<IReadOnlyList<BiometricEnrollment>> ListActiveForEmployeeAsync(
        Guid tenantId, Guid employeeId, CancellationToken cancellationToken);

    Task AddAsync(BiometricEnrollment enrollment, CancellationToken cancellationToken);
}
