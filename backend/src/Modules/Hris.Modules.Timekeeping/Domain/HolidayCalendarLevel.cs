namespace Hris.Modules.Timekeeping.Domain;

/// <summary>
/// The layering order holiday resolution walks. Source:
/// docs/04-modules/timekeeping/domain/aggregates.md's own layering diagram. Ordinal
/// order is significant: a higher value is a more specific layer, and TK-042
/// resolves a conflict to the most specific applicable layer.
/// </summary>
public enum HolidayCalendarLevel
{
    /// <summary>Platform-provided statutory layer. Read-only to the tenant (TK-041).</summary>
    Country = 0,

    Region = 1,
    Company = 2,
}
