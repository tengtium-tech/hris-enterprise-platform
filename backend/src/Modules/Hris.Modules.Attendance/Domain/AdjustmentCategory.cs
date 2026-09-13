namespace Hris.Modules.Attendance.Domain;

/// <summary>
/// Reason category explaining an <see cref="AttendanceAdjustment"/>, per
/// docs/04-modules/attendance/domain/value-objects.md. Tenant-configurable; the
/// platform ships the baseline set below.
/// </summary>
public enum AdjustmentCategory
{
    ForgotClockIn,
    ForgotClockOut,
    DeviceOffline,
    PowerFailure,
    NetworkFailure,
    OfficialBusiness,
    FieldWork,
    Emergency,
    ManagerInstruction,
    SystemError,
}
