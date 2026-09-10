namespace Hris.Modules.Timekeeping.Domain;

/// <summary>
/// The resolved holiday determination for one scope on one date, or the absence of
/// one. Distinguishing "not a holiday" from "a holiday classified thus" explicitly
/// keeps a consumer from having to treat null as a business answer.
/// </summary>
/// <param name="Holiday">The governing entry, from the most specific applicable layer.</param>
/// <param name="SourceCalendarId">Which layer supplied it, so the determination is explainable.</param>
/// <param name="SourceLevel">The level of that layer.</param>
public sealed record HolidayResolution(Holiday Holiday, HolidayCalendarId SourceCalendarId, HolidayCalendarLevel SourceLevel);

/// <summary>
/// TK-042 and TK-002 together: resolving whether a date is a holiday for a scope,
/// across the Country, Region, and Company layers, using the version of each layer
/// in force on the date being evaluated. Source:
/// docs/04-modules/timekeeping/domain/aggregates.md's layering diagram and
/// holiday-calendars.md.
///
/// Two properties matter and are easy to get wrong independently.
///
/// The first is layer precedence: where two layers classify the same date
/// differently, the more specific one governs. A Company calendar declaring a
/// national holiday a working day for its own staff is a legitimate configuration,
/// and the resolution must reflect it rather than returning the statutory
/// classification because it was found first.
///
/// The second is version selection. Every layer is independently versioned, and
/// resolution must take each layer's version effective on the evaluated date, never
/// the version current at query time. An implementation that resolved "today's"
/// version regardless of the date asked about would silently recompute history the
/// moment any calendar was revised — producing no error, and discovered only when a
/// recalculated period's numbers change for no visible reason.
/// </summary>
public static class HolidayResolver
{
    /// <summary>
    /// Resolves <paramref name="date"/> against every supplied calendar version.
    /// </summary>
    /// <param name="calendarVersions">
    /// Every version of every layer applicable to the scope. Versions not effective
    /// on <paramref name="date"/> are filtered out here rather than by the caller, so
    /// TK-002 holds even if a caller passes the full history.
    /// </param>
    /// <param name="date">The date being evaluated.</param>
    public static HolidayResolution? Resolve(IReadOnlyList<HolidayCalendar> calendarVersions, DateOnly date)
    {
        ArgumentNullException.ThrowIfNull(calendarVersions);

        HolidayResolution? resolved = null;

        foreach (var calendar in calendarVersions)
        {
            if (calendar.Status == HolidayCalendarStatus.Draft || !calendar.IsEffectiveOn(date))
            {
                continue;
            }

            var holiday = calendar.HolidayOn(date);
            if (holiday is null)
            {
                continue;
            }

            // Strictly greater: the first layer at a given level wins over a later one
            // at the same level, but any more specific layer displaces it entirely.
            if (resolved is null || calendar.Level > resolved.SourceLevel)
            {
                resolved = new HolidayResolution(holiday, calendar.Id, calendar.Level);
            }
        }

        return resolved;
    }

    /// <summary>
    /// Whether work is expected on <paramref name="date"/> for this scope, combining
    /// the schedule's working-day pattern with the holiday determination.
    ///
    /// Deliberately returns the expectation only. Whether an employee actually
    /// attended, and what any of it is worth, are <c>attendance</c> and
    /// <c>payroll</c>'s concerns respectively — this module states what was supposed
    /// to happen and computes nothing from it.
    /// </summary>
    public static bool IsWorkExpected(WorkingDayPattern pattern, HolidayResolution? holiday, DateOnly date)
    {
        ArgumentNullException.ThrowIfNull(pattern);

        if (!pattern.IsWorkingDay(date))
        {
            return false;
        }

        return holiday is null || holiday.Holiday.WorkRule != HolidayWorkRule.NoWorkExpected;
    }
}
