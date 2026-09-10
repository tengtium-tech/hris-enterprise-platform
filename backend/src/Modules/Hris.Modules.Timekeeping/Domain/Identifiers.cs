using Hris.SharedKernel;

namespace Hris.Modules.Timekeeping.Domain;

/// <summary>Identity of the <see cref="WorkSchedule"/> Aggregate Root.</summary>
public readonly record struct WorkScheduleId(Guid Value) : IStronglyTypedId;

/// <summary>
/// Identity of a <see cref="ScheduleAssignment"/>, unique within its
/// <see cref="WorkSchedule"/>. Reached only through that root; no repository exists
/// for it (CTR-ARC-004).
/// </summary>
public readonly record struct ScheduleAssignmentId(Guid Value) : IStronglyTypedId;

/// <summary>Identity of the <see cref="WorkShift"/> Aggregate Root.</summary>
public readonly record struct WorkShiftId(Guid Value) : IStronglyTypedId;

/// <summary>Identity of the <see cref="ShiftAssignment"/> Aggregate Root.</summary>
public readonly record struct ShiftAssignmentId(Guid Value) : IStronglyTypedId;

/// <summary>Identity of the <see cref="HolidayCalendar"/> Aggregate Root.</summary>
public readonly record struct HolidayCalendarId(Guid Value) : IStronglyTypedId;

/// <summary>
/// Identity of a <see cref="Holiday"/>, unique within its
/// <see cref="HolidayCalendar"/>. Reached only through that root (CTR-ARC-004).
/// </summary>
public readonly record struct HolidayId(Guid Value) : IStronglyTypedId;

/// <summary>
/// Identity of a rotation-generation configuration. The rotation configuration
/// itself is not modelled in this Sprint; the identifier exists so a
/// rotation-generated <see cref="ShiftAssignment"/> can record which cycle produced
/// it, which TK-036 requires in order for such assignments to stay individually
/// auditable rather than re-derived from the rule at read time.
/// </summary>
public readonly record struct RotationCycleId(Guid Value) : IStronglyTypedId;
