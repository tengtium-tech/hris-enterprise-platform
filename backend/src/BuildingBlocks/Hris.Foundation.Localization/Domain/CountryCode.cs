using System.Globalization;
using Hris.SharedKernel;

namespace Hris.Foundation.Localization.Domain;

/// <summary>
/// An ISO 3166-1 alpha-2 country code (e.g. "PH", "US"), the key
/// <see cref="CountryConfiguration"/> is looked up by, per localization-framework.md's
/// Country Configuration section. Validated through <see cref="RegionInfo"/>, the
/// same "let the BCL be the source of truth" approach every other identifier-format
/// type in this framework takes.
/// </summary>
public sealed class CountryCode : ValueObject
{
    public string Value { get; }

    private CountryCode(string value)
    {
        Value = value;
    }

    public static Result<CountryCode> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<CountryCode>(LocalizationErrors.CountryCodeRequired);
        }

        try
        {
            var region = new RegionInfo(value.Trim().ToUpperInvariant());
            return Result.Success(new CountryCode(region.TwoLetterISORegionName));
        }
        catch (ArgumentException)
        {
            return Result.Failure<CountryCode>(LocalizationErrors.CountryCodeInvalidFormat);
        }
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
