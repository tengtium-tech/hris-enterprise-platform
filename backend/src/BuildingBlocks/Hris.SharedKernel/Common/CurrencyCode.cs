using System.Globalization;

namespace Hris.SharedKernel;

/// <summary>
/// An ISO 4217 three-letter currency code, per
/// docs/03-foundation/localization-framework.md's Currency section ("PHP, USD, EUR,
/// GBP, SGD, JPY, AUD... Formatting should respect locale conventions") and that
/// document's own Implementation Guidance: "Treat currency as amount plus currency
/// code, never as a bare number." Paired with <see cref="Money"/>.
///
/// Validated against <see cref="RegionInfo"/>'s own ISO currency symbol table rather
/// than a hand-maintained list of "supported" codes -- this platform does not get to
/// decide which currencies are real, and the BCL already carries an accurate,
/// maintained answer. The known-code set is computed once and cached
/// (<see cref="_knownIsoCurrencyCodes"/>): scanning every specific culture on every
/// call would be a real cost against this framework's own Performance NFR, for a
/// result that never changes within a process lifetime.
/// </summary>
public sealed class CurrencyCode : ValueObject
{
    private static readonly Lazy<HashSet<string>> _knownIsoCurrencyCodes = new(BuildKnownIsoCurrencyCodes);

    public string Value { get; }

    private CurrencyCode(string value)
    {
        Value = value;
    }

    public static Result<CurrencyCode> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<CurrencyCode>(SharedKernelErrors.CurrencyCodeRequired);
        }

        var normalized = value.Trim().ToUpperInvariant();

        return _knownIsoCurrencyCodes.Value.Contains(normalized)
            ? Result.Success(new CurrencyCode(normalized))
            : Result.Failure<CurrencyCode>(SharedKernelErrors.CurrencyCodeUnrecognized);
    }

    private static HashSet<string> BuildKnownIsoCurrencyCodes()
    {
        var codes = new HashSet<string>(StringComparer.Ordinal);

        foreach (var culture in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
        {
            try
            {
                codes.Add(new RegionInfo(culture.Name).ISOCurrencySymbol);
            }
            catch (ArgumentException)
            {
                // A handful of specific cultures (e.g. some invariant/custom ones)
                // have no corresponding RegionInfo; skip rather than fail the whole
                // lookup table over cultures this platform will never target anyway.
            }
        }

        return codes;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
