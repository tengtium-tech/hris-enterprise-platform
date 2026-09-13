namespace Hris.Modules.Attendance.Domain;

/// <summary>
/// The physical or virtual kind of an <see cref="AttendanceDevice"/>. Source:
/// docs/04-modules/attendance/domain/aggregates.md (AttendanceDevice). The platform
/// baseline set; a tenant may not reconfigure the enum.
/// </summary>
public enum AttendanceDeviceType
{
    Biometric,
    RfidCard,
    MobileApplication,
    WebPortal,
    ApiIntegration,
}
