namespace Hris.Modules.Attendance.Domain;

/// <summary>
/// Configured duration, part of <see cref="AttendancePolicy"/>, within which a late
/// clock-in is not classified as tardiness (AT-004), per
/// docs/04-modules/attendance/domain/value-objects.md. Expressed in minutes.
/// </summary>
public readonly record struct GracePeriod
{
    public int Minutes { get; }

    private GracePeriod(int minutes) => Minutes = minutes;

    public static GracePeriod FromMinutes(int minutes) => new(minutes);
}
