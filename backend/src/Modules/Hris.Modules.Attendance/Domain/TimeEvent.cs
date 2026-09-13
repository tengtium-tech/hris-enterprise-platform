using Hris.SharedKernel;

namespace Hris.Modules.Attendance.Domain;

/// <summary>
/// One immutable captured punch, owned by <see cref="AttendanceRecord"/>. Source:
/// docs/04-modules/attendance/domain/entities.md (TimeEvent).
///
/// A time event is never edited after capture; a correction is a new
/// <see cref="AttendanceAdjustment"/>, never a mutation of this entity (AT-001). Its
/// calculation contribution (which break it closes, which shift period it falls in) is
/// derived at read time by <see cref="AttendanceCalculationEngine"/> from the complete
/// ordered event set, never stored here, so a recalculation can always reproduce it.
/// </summary>
public sealed class TimeEvent : Entity<TimeEventId>
{
    public TimeEventType EventType { get; }

    /// <summary>Captured with the originating device or channel clock, normalized to UTC.</summary>
    public DateTimeOffset TimestampUtc { get; }

    public AttendanceSource Source { get; }

    /// <summary>Originating <see cref="AttendanceDevice"/> identifier, where the source is a registered device.</summary>
    public Guid? AttendanceDeviceId { get; }

    /// <summary>Raw captured value — never altered after write.</summary>
    public string? RawValue { get; }

    /// <summary>The time zone the originating clock was in, preserved so the UTC instant is explainable.</summary>
    public string? OriginatingTimeZone { get; }

    internal TimeEvent(
        TimeEventId id, TimeEventType eventType, DateTimeOffset timestampUtc, AttendanceSource source,
        Guid? attendanceDeviceId, string? rawValue, string? originatingTimeZone)
        : base(id)
    {
        EventType = eventType;
        TimestampUtc = timestampUtc;
        Source = source;
        AttendanceDeviceId = attendanceDeviceId;
        RawValue = rawValue;
        OriginatingTimeZone = originatingTimeZone;
    }
}
