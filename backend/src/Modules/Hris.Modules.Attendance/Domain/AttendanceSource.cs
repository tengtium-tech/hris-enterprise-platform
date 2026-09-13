namespace Hris.Modules.Attendance.Domain;

/// <summary>
/// Origin of a captured <see cref="TimeEvent"/>, per
/// docs/04-modules/attendance/domain/value-objects.md. Preserved permanently on the
/// event; never overwritten by a later correction (AT-001).
/// </summary>
public enum AttendanceSource
{
    BiometricDevice,
    RfidCardDevice,
    MobileApplication,
    WebPortal,
    ManualEntry,
    ApiIntegration,
    ImportedFile,
}
