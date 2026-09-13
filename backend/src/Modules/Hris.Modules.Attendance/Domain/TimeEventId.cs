using Hris.SharedKernel;

namespace Hris.Modules.Attendance.Domain;

/// <summary>
/// Strongly typed identifier for the <see cref="TimeEvent"/> child entity, unique within
/// its owning <see cref="AttendanceRecord"/>.
/// </summary>
public readonly record struct TimeEventId(Guid Value) : IStronglyTypedId;
