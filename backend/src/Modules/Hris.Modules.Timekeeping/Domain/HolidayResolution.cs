namespace Hris.Modules.Timekeeping.Domain;

/// <summary>
/// The resolved holiday determination for one scope on one date, or the absence of
/// one. Distinguishing "not a holiday" from "a holiday classified thus" explicitly
/// keeps a consumer from having to treat null as a business answer.
/// </summary>
/// <param name="Holiday">The governing entry, from the most specific applicable layer.</param>
/// <param name="SourceCalendarId">Which layer supplied it, so the determination is explainable.</param>
/// <param name="SourceLevel">The level of that layer.</param>
public sealed record HolidayResolution(
    Holiday Holiday, HolidayCalendarId SourceCalendarId, HolidayCalendarLevel SourceLevel);
