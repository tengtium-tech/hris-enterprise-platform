namespace Hris.Modules.Timekeeping.Domain;

/// <summary>
/// Country-scoped holiday classification. Source:
/// docs/04-modules/timekeeping/domain/value-objects.md.
///
/// The three-way statutory split is the Philippine DOLE classification
/// specifically, not a universal taxonomy. A future country configuration may define
/// an entirely different set; TK-043 validates a value against the vocabulary
/// defined for the calendar's own country rather than assuming this set applies
/// everywhere.
/// </summary>
public enum HolidayType
{
    RegularHoliday = 0,
    SpecialNonWorkingHoliday = 1,
    SpecialWorkingHoliday = 2,
    LocalHoliday = 3,
    CompanyHoliday = 4,
    FloatingHoliday = 5,
}
