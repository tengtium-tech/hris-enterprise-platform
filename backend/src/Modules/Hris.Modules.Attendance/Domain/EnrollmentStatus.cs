namespace Hris.Modules.Attendance.Domain;

/// <summary>
/// Lifecycle of a <see cref="BiometricEnrollment"/>, per
/// docs/04-modules/attendance/domain/aggregates.md. Revocation propagates to every
/// synchronized device before it is considered complete (AT-053).
/// </summary>
public enum EnrollmentStatus
{
    Enrolled,
    Active,
    Revoked,
}
