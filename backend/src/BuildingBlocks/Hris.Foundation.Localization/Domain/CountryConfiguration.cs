using Hris.SharedKernel;

namespace Hris.Foundation.Localization.Domain;

/// <summary>
/// One country's localization defaults, per localization-framework.md's Country
/// Configuration section and its own constraint TC-008 note: "Philippine rules are
/// the first country configuration, not embedded logic." Realizes "Default Currency,
/// Default Language, Time Zone, Working Days, Weekend Definition, Address Format,
/// Phone Format" from that section.
///
/// <b>Deliberately excludes "National Holidays"</b>, the one item that section also
/// names: IMPLEMENTATION-PLAN.md's own Phase 3 Sprint 3 assigns "Holidays" to the
/// `timekeeping` module ("Scheduling; Shift Management; Holidays"), not to this
/// Sprint 3 Foundation framework. A holiday calendar is recurring, year-specific data
/// with its own entity shape; modeling it here would be reaching into a later
/// module's own scope, exactly what this project's Coding Phase conventions exist to
/// prevent. <see cref="Country"/> is what `timekeeping`'s own holiday calendar will
/// key against once built.
/// </summary>
public sealed class CountryConfiguration : AggregateRoot<CountryConfigurationId>
{
    private readonly HashSet<DayOfWeek> _workingDays;

    public CountryCode Country { get; }

    public CurrencyCode DefaultCurrency { get; private set; }

    public LanguageCode DefaultLanguage { get; private set; }

    public TimeZoneId DefaultTimeZone { get; private set; }

    public IReadOnlySet<DayOfWeek> WorkingDays => _workingDays;

    public string AddressFormat { get; private set; }

    public string PhoneFormat { get; private set; }

    private CountryConfiguration(
        CountryConfigurationId id,
        CountryCode country,
        CurrencyCode defaultCurrency,
        LanguageCode defaultLanguage,
        TimeZoneId defaultTimeZone,
        IReadOnlyCollection<DayOfWeek> workingDays,
        string addressFormat,
        string phoneFormat)
        : base(id)
    {
        Country = country;
        DefaultCurrency = defaultCurrency;
        DefaultLanguage = defaultLanguage;
        DefaultTimeZone = defaultTimeZone;
        _workingDays = [.. workingDays];
        AddressFormat = addressFormat;
        PhoneFormat = phoneFormat;
    }

    public static Result<CountryConfiguration> Create(
        CountryCode country,
        CurrencyCode defaultCurrency,
        LanguageCode defaultLanguage,
        TimeZoneId defaultTimeZone,
        IReadOnlyCollection<DayOfWeek> workingDays,
        string addressFormat,
        string phoneFormat)
    {
        Guard.AgainstNull(country, nameof(country));
        Guard.AgainstNull(defaultCurrency, nameof(defaultCurrency));
        Guard.AgainstNull(defaultLanguage, nameof(defaultLanguage));
        Guard.AgainstNull(defaultTimeZone, nameof(defaultTimeZone));

        return Result.Success(new CountryConfiguration(
            new CountryConfigurationId(Guid.NewGuid()),
            country,
            defaultCurrency,
            defaultLanguage,
            defaultTimeZone,
            workingDays,
            addressFormat,
            phoneFormat));
    }

    public void UpdateDefaultCurrency(CurrencyCode currency, DateTimeOffset nowUtc)
    {
        DefaultCurrency = Guard.AgainstNull(currency, nameof(currency));
        AddDomainEvent(new CurrencyConfigured(Guid.NewGuid(), nowUtc, Country, currency));
    }

    public void UpdateDefaultLanguage(LanguageCode language, DateTimeOffset nowUtc)
    {
        DefaultLanguage = Guard.AgainstNull(language, nameof(language));
        AddDomainEvent(new LanguageChanged(Guid.NewGuid(), nowUtc, Country, language));
    }

    public void UpdateDefaultTimeZone(TimeZoneId timeZone, DateTimeOffset nowUtc)
    {
        DefaultTimeZone = Guard.AgainstNull(timeZone, nameof(timeZone));
        AddDomainEvent(new TimeZoneChanged(Guid.NewGuid(), nowUtc, Country, timeZone));
    }

    public void UpdateWorkingDays(IReadOnlyCollection<DayOfWeek> workingDays, DateTimeOffset nowUtc)
    {
        Guard.AgainstNull(workingDays, nameof(workingDays));
        _workingDays.Clear();
        foreach (var day in workingDays)
        {
            _workingDays.Add(day);
        }

        AddDomainEvent(new CountryConfigurationUpdated(Guid.NewGuid(), nowUtc, Country));
    }

    public void UpdateFormats(string addressFormat, string phoneFormat, DateTimeOffset nowUtc)
    {
        AddressFormat = Guard.AgainstNullOrWhiteSpace(addressFormat, nameof(addressFormat));
        PhoneFormat = Guard.AgainstNullOrWhiteSpace(phoneFormat, nameof(phoneFormat));
        AddDomainEvent(new CountryConfigurationUpdated(Guid.NewGuid(), nowUtc, Country));
    }
}
