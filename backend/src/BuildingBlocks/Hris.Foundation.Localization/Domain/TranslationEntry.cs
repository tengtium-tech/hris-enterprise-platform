using Hris.Foundation.Identity.Domain;
using Hris.SharedKernel;

namespace Hris.Foundation.Localization.Domain;

/// <summary>
/// One translatable string and every locale it has been translated into, per
/// localization-framework.md's Translation Management section ("Translation Keys...
/// Dynamic Translations, Version Management, Missing Translation Detection, Fallback
/// Languages... should support versioning").
///
/// Versioning here is a single incrementing counter over the whole entry, not the
/// Draft/Validated/Published/Active/Deprecated/Archived lifecycle
/// <c>ConfigurationSetting</c> and <c>RuleDefinition</c> use: this document states
/// only "should support versioning" for translations, with no lifecycle diagram the
/// way Configuration Framework and Rules Engine each have their own -- reusing that
/// heavier shape here would be padding this aggregate out to match a sibling
/// framework's own documented complexity rather than what this one actually
/// specifies.
/// </summary>
public sealed class TranslationEntry : AggregateRoot<TranslationEntryId>
{
    private readonly Dictionary<string, string> _translationsByLocale;

    public string Key { get; }

    public int VersionNumber { get; private set; }

    public IReadOnlyDictionary<string, string> TranslationsByLocale => _translationsByLocale;

    private TranslationEntry(TranslationEntryId id, string key)
        : base(id)
    {
        Key = key;
        _translationsByLocale = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        VersionNumber = 0;
    }

    public static Result<TranslationEntry> Create(string? key, Locale locale, string? text, UserAccountId updatedBy, DateTimeOffset nowUtc)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return Result.Failure<TranslationEntry>(LocalizationErrors.TranslationKeyRequired);
        }

        var entry = new TranslationEntry(new TranslationEntryId(Guid.NewGuid()), key.Trim());

        var setResult = entry.SetTranslation(locale, text, updatedBy, nowUtc);
        return setResult.IsFailure ? Result.Failure<TranslationEntry>(setResult.Error) : Result.Success(entry);
    }

    public Result SetTranslation(Locale locale, string? text, UserAccountId updatedBy, DateTimeOffset nowUtc)
    {
        Guard.AgainstNull(locale, nameof(locale));

        if (string.IsNullOrWhiteSpace(text))
        {
            return Result.Failure(LocalizationErrors.TranslatedTextRequired);
        }

        _translationsByLocale[locale.Value] = text;
        VersionNumber++;
        AddDomainEvent(new TranslationUpdated(Guid.NewGuid(), nowUtc, Id, Key, VersionNumber));
        return Result.Success();
    }

    /// <summary>
    /// Tries <paramref name="locale"/> first, then each entry in
    /// <paramref name="fallbackChain"/> in order, per this document's own "Fallback
    /// Languages... Fallback behavior should be configurable" -- the caller supplies
    /// the configured chain (e.g. tenant default, then platform default) rather than
    /// this type assuming one.
    /// </summary>
    public Result<string> Resolve(Locale locale, IReadOnlyList<Locale> fallbackChain)
    {
        Guard.AgainstNull(locale, nameof(locale));
        Guard.AgainstNull(fallbackChain, nameof(fallbackChain));

        if (_translationsByLocale.TryGetValue(locale.Value, out var exact))
        {
            return Result.Success(exact);
        }

        foreach (var fallback in fallbackChain)
        {
            if (_translationsByLocale.TryGetValue(fallback.Value, out var fallbackText))
            {
                return Result.Success(fallbackText);
            }
        }

        return Result.Failure<string>(LocalizationErrors.NoTranslationAvailable);
    }
}
