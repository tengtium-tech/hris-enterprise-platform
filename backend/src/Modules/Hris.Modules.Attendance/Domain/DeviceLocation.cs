namespace Hris.Modules.Attendance.Domain;

/// <summary>
/// Identifies where an <see cref="AttendanceDevice"/> is deployed, by reference into the
/// organization module, per docs/04-modules/attendance/domain/value-objects.md. Never
/// duplicated as descriptive text that can drift from the organizational record it
/// describes.
/// </summary>
public readonly record struct DeviceLocation
{
    public string ReferenceId { get; }

    private DeviceLocation(string referenceId) => ReferenceId = referenceId;

    public static DeviceLocation FromReference(string referenceId) => new(referenceId);
}
