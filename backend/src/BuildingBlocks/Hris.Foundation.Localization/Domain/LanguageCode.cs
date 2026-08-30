using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Hris.SharedKernel;

namespace Hris.Foundation.Localization.Domain;

/// <summary>
/// An ISO 639-1 two-letter language code, per localization-framework.md's Language
/// section ("English, Filipino, Japanese, Chinese, French, German, Spanish...
/// configurable"). Validated via <see cref="CultureInfo.TwoLetterISOLanguageName"/>
/// against the BCL's neutral-culture table, the same "let the BCL be the source of
/// truth" approach <see cref="Locale"/> takes.
/// </summary>
public sealed class LanguageCode : ValueObject
{
    private static readonly Lazy<HashSet<string>> _knownLanguageCodes = new(() =>
        new HashSet<string>(
            CultureInfo.GetCultures(CultureTypes.NeutralCultures).Select(c => c.TwoLetterISOLanguageName),
            StringComparer.Ordinal));

    public string Value { get; }

    private LanguageCode(string value)
    {
        Value = value;
    }

    [SuppressMessage(
        "Globalization",
        "CA1308:Normalize strings to uppercase",
        Justification = "ISO 639-1 language codes are conventionally lowercase; see "
            + "EmailAddress.Create's identical, more fully justified suppression.")]
    public static Result<LanguageCode> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<LanguageCode>(LocalizationErrors.LanguageCodeRequired);
        }

        var normalized = value.Trim().ToLowerInvariant();

        return _knownLanguageCodes.Value.Contains(normalized)
            ? Result.Success(new LanguageCode(normalized))
            : Result.Failure<LanguageCode>(LocalizationErrors.LanguageCodeUnrecognized);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
