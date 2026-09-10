using Hris.SharedKernel;

namespace Hris.Modules.Timekeeping.Domain;

/// <summary>
/// Identity of a <see cref="Holiday"/>, unique within its
/// <see cref="HolidayCalendar"/>. Reached only through that root (CTR-ARC-004).
/// </summary>
public readonly record struct HolidayId(Guid Value) : IStronglyTypedId;
