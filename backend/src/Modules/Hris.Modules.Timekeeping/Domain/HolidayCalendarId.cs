using Hris.SharedKernel;

namespace Hris.Modules.Timekeeping.Domain;

/// <summary>
/// Identity of the <see cref="HolidayCalendar"/> Aggregate Root.
/// </summary>
public readonly record struct HolidayCalendarId(Guid Value) : IStronglyTypedId;
