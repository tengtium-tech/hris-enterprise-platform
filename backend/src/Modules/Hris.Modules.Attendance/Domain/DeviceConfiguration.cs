namespace Hris.Modules.Attendance.Domain;

/// <summary>
/// Operational configuration for an <see cref="AttendanceDevice"/>, changed by
/// <c>ConfigureAttendanceDeviceCommand</c>. Source:
/// docs/04-modules/attendance/application/commands.md (AttendanceDevice Commands).
/// Stored as a value object on the device; every field is optional so a partial
/// reconfiguration need not clear the rest.
/// </summary>
public readonly record struct DeviceConfiguration(
    string? TimeZoneId,
    int? HeartbeatIntervalSeconds,
    bool? AutoOfflineDetection);
