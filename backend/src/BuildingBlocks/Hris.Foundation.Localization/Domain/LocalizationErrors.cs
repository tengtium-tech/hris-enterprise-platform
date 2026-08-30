using Hris.SharedKernel;

namespace Hris.Foundation.Localization.Domain;

/// <summary>
/// This bounded context's own reusable error catalog, per error-pattern.md's "Error
/// Catalog" section.
/// </summary>
public static class LocalizationErrors
{
    public static readonly Error LocaleRequired = new(
        "Localization.LocaleRequired",
        "A locale is required.",
        ErrorCategory.Validation);

    public static readonly Error LocaleUnrecognized = new(
        "Localization.LocaleUnrecognized",
        "The locale is not a recognized culture tag.",
        ErrorCategory.Validation);

    public static readonly Error LanguageCodeRequired = new(
        "Localization.LanguageCodeRequired",
        "A language code is required.",
        ErrorCategory.Validation);

    public static readonly Error LanguageCodeUnrecognized = new(
        "Localization.LanguageCodeUnrecognized",
        "The language code is not a recognized ISO 639-1 code.",
        ErrorCategory.Validation);

    public static readonly Error TimeZoneIdRequired = new(
        "Localization.TimeZoneIdRequired",
        "A time zone id is required.",
        ErrorCategory.Validation);

    public static readonly Error TimeZoneIdUnrecognized = new(
        "Localization.TimeZoneIdUnrecognized",
        "The time zone id is not recognized.",
        ErrorCategory.Validation);

    public static readonly Error CountryCodeRequired = new(
        "Localization.CountryCodeRequired",
        "A country code is required.",
        ErrorCategory.Validation);

    public static readonly Error CountryCodeInvalidFormat = new(
        "Localization.CountryCodeInvalidFormat",
        "A country code must be a two-letter ISO 3166-1 alpha-2 code.",
        ErrorCategory.Validation);

    public static readonly Error TranslationKeyRequired = new(
        "Localization.TranslationKeyRequired",
        "A translation key is required.",
        ErrorCategory.Validation);

    public static readonly Error TranslatedTextRequired = new(
        "Localization.TranslatedTextRequired",
        "Translated text cannot be empty.",
        ErrorCategory.Validation);

    public static readonly Error NoTranslationAvailable = new(
        "Localization.NoTranslationAvailable",
        "No translation is available for the requested locale, and no fallback resolved one either.",
        ErrorCategory.NotFound);
}
