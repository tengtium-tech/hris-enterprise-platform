namespace Hris.Modules.Attendance.Domain;

/// <summary>
/// Operational state of an <see cref="AttendanceDevice"/>, per
/// docs/04-modules/attendance/domain/attendance-devices.md. Only <see cref="Active"/>
/// may submit time events accepted by <see cref="AttendanceRecord"/> (AT-050).
/// </summary>
public enum DeviceStatus
{
    Active,
    Inactive,
    Maintenance,
    Offline,
    Retired,
}
