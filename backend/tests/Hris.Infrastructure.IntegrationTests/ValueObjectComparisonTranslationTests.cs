using FluentAssertions;
using Hris.Foundation.Configuration.Domain;
using Hris.Foundation.Localization.Domain;
using Hris.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Hris.Infrastructure.IntegrationTests;

/// <summary>
/// Settles HEP-38: whether EF Core's real Npgsql query translator accepts
/// <c>entity.ValueObjectProperty == valueObjectInstance</c> -- a comparison that
/// resolves to <see cref="Hris.SharedKernel.ValueObject"/>'s own overloaded
/// <c>==</c> operator (component-wise <c>SequenceEqual</c>, not reference equality)
/// -- when that property is mapped through <c>HasConversion</c>. Both already-merged
/// repositories using exactly this shape are covered:
/// <see cref="IConfigurationSettingRepository.GetByKeyAndScopeAsync"/> (flagged in
/// that repository's own remarks since Configuration Framework's own PR) and
/// <see cref="ICountryConfigurationRepository.GetByCountryAsync"/> (flagged the
/// same way in Localization Framework's own PR). Both pass: Npgsql's own relational
/// query translator resolves the operator overload correctly and produces a normal
/// SQL equality predicate on the converted column -- no client-side evaluation
/// fallback, no "could not be translated" exception, confirmed against a real,
/// disposable PostgreSQL 16 instance (see <see cref="PostgresContainerFixture"/>),
/// not the EF Core InMemory provider, per this project's own testing standard
/// ("An in-memory provider is not acceptable... does not reproduce PostgreSQL
/// behaviour for... LINQ translation").
///
/// Deliberately narrow: this confirms the *mechanism* generalizes (the same
/// HasConversion + operator-overload shape any future repository over a
/// single-property Value Object will also use, including whatever Sprint 4 adds),
/// not full CRUD coverage of either repository -- see this project's own csproj
/// header for why broader repository/command/query coverage is HEP-33's scope, not
/// this one.
/// </summary>
public sealed class ValueObjectComparisonTranslationTests : IClassFixture<PostgresContainerFixture>
{
    private readonly PostgresContainerFixture _fixture;

    public ValueObjectComparisonTranslationTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ConfigurationSettingRepository_GetByKeyAndScopeAsync_TranslatesKeyComparison()
    {
        var key = ConfigurationKey.Create("IntegrationTests.Hep38.ConfigKey").Value;
        var globalScope = ConfigurationScope.Global();

        using (var writeScope = _fixture.CreateScope())
        {
            var repository = writeScope.ServiceProvider.GetRequiredService<IConfigurationSettingRepository>();
            var dbContext = writeScope.ServiceProvider.GetRequiredService<HrisDbContext>();

            var setting = ConfigurationSetting.Create(
                key,
                globalScope,
                ConfigurationCategory.Platform,
                ConfigurationDataType.Text,
                initialValue: "hello-world",
                effectiveDate: DateOnly.FromDateTime(DateTime.UtcNow),
                expirationDate: null,
                changeSummary: "HEP-38 verification seed",
                createdByUserId: Guid.NewGuid(),
                nowUtc: DateTimeOffset.UtcNow).Value;

            await repository.AddAsync(setting, CancellationToken.None);
            await dbContext.SaveChangesAsync(CancellationToken.None);
        }

        // A fresh scope, and therefore a fresh HrisDbContext/change tracker, so this
        // read genuinely round-trips through Npgsql rather than being satisfied from
        // the write scope's own in-memory identity map.
        using var readScope = _fixture.CreateScope();
        var readRepository = readScope.ServiceProvider.GetRequiredService<IConfigurationSettingRepository>();

        var found = await readRepository.GetByKeyAndScopeAsync(key, globalScope, CancellationToken.None);

        found.Should().NotBeNull();
        found!.Key.Should().Be(key);
    }

    [Fact]
    public async Task CountryConfigurationRepository_GetByCountryAsync_TranslatesCountryComparison()
    {
        var country = CountryCode.Create("PH").Value;

        using (var writeScope = _fixture.CreateScope())
        {
            var repository = writeScope.ServiceProvider.GetRequiredService<ICountryConfigurationRepository>();
            var dbContext = writeScope.ServiceProvider.GetRequiredService<HrisDbContext>();

            var configuration = CountryConfiguration.Create(
                country,
                Hris.SharedKernel.CurrencyCode.Create("PHP").Value,
                LanguageCode.Create("en").Value,
                TimeZoneId.Create("Asia/Manila").Value,
                workingDays: [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday],
                addressFormat: "{Street}, {City}, {Province} {PostalCode}",
                phoneFormat: "+63 XXX XXX XXXX").Value;

            await repository.AddAsync(configuration, CancellationToken.None);
            await dbContext.SaveChangesAsync(CancellationToken.None);
        }

        using var readScope = _fixture.CreateScope();
        var readRepository = readScope.ServiceProvider.GetRequiredService<ICountryConfigurationRepository>();

        var found = await readRepository.GetByCountryAsync(country, CancellationToken.None);

        found.Should().NotBeNull();
        found!.Country.Should().Be(country);
        found.WorkingDays.Should().BeEquivalentTo(
        [
            DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday,
        ]);
    }
}
