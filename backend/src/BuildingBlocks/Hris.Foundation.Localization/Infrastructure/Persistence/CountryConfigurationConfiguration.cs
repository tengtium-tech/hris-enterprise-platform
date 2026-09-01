using Hris.Foundation.Localization.Domain;
using Hris.Infrastructure.Persistence;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hris.Foundation.Localization.Infrastructure.Persistence;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="CountryConfiguration"/>
/// Aggregate Root, per coding-standards.md's Infrastructure Layer convention.
///
/// Every Value Object here (<see cref="Domain.CountryConfiguration.Country"/>,
/// <c>DefaultCurrency</c>, <c>DefaultLanguage</c>, <c>DefaultTimeZone</c>, and
/// <c>WorkingDays</c>) is mapped through <c>HasConversion</c> rather than
/// <c>OwnsOne</c>/<c>OwnsMany</c>, a deliberate choice this Sprint's own EF Core
/// owned-type constructor-binding fix (see that commit's own body) makes with the
/// benefit of hindsight: only a scalar or converted property can be
/// constructor-bound, and an owned-type mapping for any of these five would
/// reproduce that exact defect.
///
/// <c>HasConversion</c> alone was not sufficient for <c>WorkingDays</c>, though:
/// verifying this mapping with a real EF Core model build (the same harness that
/// found this Sprint's own owned-type bugs) surfaced a second, different
/// constructor-binding failure mode -- the constructor parameter's own type
/// (<c>IReadOnlyCollection&lt;DayOfWeek&gt;</c>) does not match the mapped
/// property's own type (<c>IReadOnlySet&lt;DayOfWeek&gt;</c>), and EF Core requires
/// an exact match to bind a parameter to a property regardless of conversion. See
/// <see cref="Domain.CountryConfiguration"/>'s own second, EF-Core-only constructor
/// for the fix -- the identical additive-constructor remedy, just triggered by a
/// type mismatch rather than an owned-type navigation.
/// </summary>
public sealed class CountryConfigurationConfiguration : IEntityTypeConfiguration<CountryConfiguration>
{
    public void Configure(EntityTypeBuilder<CountryConfiguration> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasKey(configuration => configuration.Id);

        builder.Property(configuration => configuration.Id)
            .HasConversion(new StronglyTypedIdValueConverter<CountryConfigurationId>(value => new CountryConfigurationId(value)))
            .ValueGeneratedNever();

        // CountryCode: the natural key ICountryConfigurationRepository actually
        // looks up by (that interface's own remarks) -- one configuration per
        // country, per this aggregate's own Create uniqueness check.
        builder.Property(configuration => configuration.Country)
            .HasConversion(
                country => country.Value,
                value => CountryCode.Create(value).Value)
            .HasMaxLength(2)
            .IsRequired();

        builder.HasIndex(configuration => configuration.Country).IsUnique();

        builder.Property(configuration => configuration.DefaultCurrency)
            .HasConversion(
                currency => currency.Value,
                value => CurrencyCode.Create(value).Value)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(configuration => configuration.DefaultLanguage)
            .HasConversion(
                language => language.Value,
                value => LanguageCode.Create(value).Value)
            .HasMaxLength(2)
            .IsRequired();

        builder.Property(configuration => configuration.DefaultTimeZone)
            .HasConversion(
                timeZone => timeZone.Value,
                value => TimeZoneId.Create(value).Value)
            .HasMaxLength(100)
            .IsRequired();

        // WorkingDays: a HashSet<DayOfWeek>, stored as a comma-separated list of the
        // underlying integer day values -- a plain delimited string, not JSON, since
        // DayOfWeek is already a closed BCL enum with no nested structure JSON would
        // buy anything for. PropertyAccessMode.Field is required here (not merely
        // defensive, unlike TranslationsByLocale's own identical declaration):
        // WorkingDays is no longer constructor-bound (see this class's own remarks
        // above), so EF Core must populate it post-construction, and the property's
        // own getter is an expression body reading the named field directly, with
        // no usable setter.
        builder.Property(configuration => configuration.WorkingDays)
            .HasConversion(
                days => string.Join(',', days.Select(day => (int)day)),
                value => string.IsNullOrEmpty(value)
                    ? new HashSet<DayOfWeek>()
                    : value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(part => (DayOfWeek)int.Parse(part, System.Globalization.CultureInfo.InvariantCulture))
                        .ToHashSet())
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .IsRequired();

        builder.Property(configuration => configuration.AddressFormat).IsRequired();
        builder.Property(configuration => configuration.PhoneFormat).IsRequired();
    }
}
