namespace Hris.Modules.Attendance.Domain;

/// <summary>
/// Biometric modality an <see cref="BiometricEnrollment"/> registers for, per
/// docs/04-modules/attendance/domain/value-objects.md. The platform stores only a
/// non-reversible reference for the chosen method (AT-052).
/// </summary>
public enum BiometricMethod
{
    Fingerprint,
    Face,
    Iris,
    Palm,
}
