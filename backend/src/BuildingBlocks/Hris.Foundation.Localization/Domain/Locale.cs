using System.Globalization;
using Hris.SharedKernel;

namespace Hris.Foundation.Localization.Domain;

/// <summary>
/// A BCP-47 locale tag, per localization-framework.md's Locale section ("en-US,
/// en-GB, en-PH, fil-PH, ja-JP... controls formatting, language, and cultural
/// conventions"). Validated by resolving it as a real <see cref="CultureInfo"/>
/// rather than a hand-maintained allow-list -- the BCL's own culture table is the
/// authoritative source for "is this a real locale," and this platform gains nothing
/// by re-deriving it.
///
/// Deliberately a separate concept from <see cref="LanguageCode"/>: this document's
/// own Language section states "Users may choose their preferred language
/// independently of their location," so a locale (formatting/regional convention)
/// and a language preference are not collapsed into one type here.
/// </summary>
public sealed class Locale : ValueObject
{
    public string Value { get; }

    private Locale(string value)
    {
        Value = value;
    }

    public static Result<Locale> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<Locale>(LocalizationErrors.LocaleRequired);
        }

        try
        {
            var culture = CultureInfo.GetCultureInfo(value.Trim());
            return Result.Success(new Locale(culture.Name));
        }
        catch (CultureNotFoundException)
        {
            return Result.Failure<Locale>(LocalizationErrors.LocaleUnrecognized);
        }
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
