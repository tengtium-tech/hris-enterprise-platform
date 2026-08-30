using Hris.SharedKernel;

namespace Hris.Foundation.Localization.Domain;

/// <summary>
/// Five of localization-framework.md's own six listed Domain Events.
/// <c>LocaleChanged</c> is not implemented: this document's own Locale section
/// separates locale (regional/formatting convention) from language, currency, and
/// time zone, but the only aggregates this Sprint 3 pass builds --
/// <see cref="TranslationEntry"/> and <see cref="CountryConfiguration"/> -- have no
/// field whose change that event would describe. It most plausibly belongs to a
/// per-user locale *preference* concept this document gestures at ("Resolve locale
/// from tenant and user preference") but never specifies as its own aggregate;
/// building one speculatively, with no described shape beyond "a user preference,"
/// would be inventing where this document is silent. If Identity Framework's
/// <c>UserAccount</c> later grows a locale preference field, that is where this
/// event belongs.
/// </summary>
public sealed record TranslationUpdated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    TranslationEntryId TranslationEntryId,
    string Key,
    int VersionNumber) : IDomainEvent;

public sealed record LanguageChanged(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    CountryCode CountryCode,
    LanguageCode NewDefaultLanguage) : IDomainEvent;

public sealed record CurrencyConfigured(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    CountryCode CountryCode,
    CurrencyCode NewDefaultCurrency) : IDomainEvent;

public sealed record TimeZoneChanged(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    CountryCode CountryCode,
    TimeZoneId NewDefaultTimeZone) : IDomainEvent;

public sealed record CountryConfigurationUpdated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    CountryCode CountryCode) : IDomainEvent;
