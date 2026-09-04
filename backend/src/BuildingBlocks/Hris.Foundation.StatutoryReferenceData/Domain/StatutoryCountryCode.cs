using System.Globalization;
using Hris.SharedKernel;

namespace Hris.Foundation.StatutoryReferenceData.Domain;

/// <summary>
/// An ISO 3166-1 alpha-2 country code (e.g. "PH"), the axis statutory-reference-data.md's
/// own Country Scoping section scopes every <see cref="StatutoryProgram"/> by ("Statutory
/// Reference Data is scoped by country, never by tenant"). Validated through
/// <see cref="RegionInfo"/>, the identical "let the BCL be the source of truth" approach
/// <c>Hris.Foundation.Localization.Domain.CountryCode</c> already takes -- duplicated
/// locally rather than referenced across a `ProjectReference`, the same "each framework
/// declares its own Value Objects, even ones that conceptually overlap with a sibling
/// framework's own" choice <c>ScheduleTimeZone</c> already makes relative to
/// Localization's own <c>TimeZoneId</c>; no Sprint 4 framework in this solution takes a
/// `ProjectReference` on another Sprint 3/4 framework's own project.
/// </summary>
public sealed class StatutoryCountryCode : ValueObject
{
    public string Value { get; }

    private StatutoryCountryCode(string value)
    {
        Value = value;
    }

    public static Result<StatutoryCountryCode> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<StatutoryCountryCode>(StatutoryReferenceDataErrors.CountryCodeRequired);
        }

        try
        {
            var region = new RegionInfo(value.Trim().ToUpperInvariant());
            return Result.Success(new StatutoryCountryCode(region.TwoLetterISORegionName));
        }
        catch (ArgumentException)
        {
            return Result.Failure<StatutoryCountryCode>(StatutoryReferenceDataErrors.CountryCodeInvalidFormat);
        }
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
