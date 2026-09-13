namespace Hris.Modules.Attendance.Application.Dtos;

/// <summary>
/// Read shape for <c>AttendanceDevice</c>: identification, location, status, and health.
/// Deliberately carries no captured-event history inline (dto-design.md) — that belongs to
/// the records the device feeds, not to the device's own read model.
/// </summary>
public sealed record AttendanceDeviceDto(
    Guid Id,
    Guid TenantId,
    string Name,
    string SerialNumber,
    string? Manufacturer,
    string? Model,
    string? FirmwareVersion,
    string Type,
    string? LocationReferenceId,
    DeviceConfigurationDto? Configuration,
    string Status,
    DateTimeOffset? LastSynchronizedUtc);

/// <summary>Operational configuration of a device, copied from <c>DeviceConfiguration</c>.</summary>
public sealed record DeviceConfigurationDto(
    string? TimeZoneId,
    int? HeartbeatIntervalSeconds,
    bool? AutoOfflineDetection);
