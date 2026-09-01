using FluentAssertions;
using Hris.Foundation.Audit.Domain;
using Hris.Foundation.Authorization.Domain;
using Hris.Foundation.Configuration.Domain;
using Hris.Foundation.Events.Domain;
using Hris.Foundation.Identity.Domain;
using Hris.Foundation.Localization.Domain;
using Hris.Foundation.RulesEngine.Domain;
using Hris.Infrastructure.Persistence;
using Hris.SharedKernel;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Hris.Infrastructure.IntegrationTests;

/// <summary>
/// Whether EF Core's real Npgsql query translator accepts the LINQ predicate shapes
/// this codebase's own repositories already write, but had never run against a real
/// PostgreSQL instance to confirm. Two related but distinct risks, both covered
/// here:
///
/// <list type="bullet">
/// <item>A converted Value Object compared with <c>==</c>, which resolves to
/// <see cref="ValueObject"/>'s own overloaded operator (component-wise
/// <c>SequenceEqual</c>, not reference equality) rather than a simple scalar
/// comparison -- <see cref="IConfigurationSettingRepository.GetByKeyAndScopeAsync"/>,
/// <see cref="ICountryConfigurationRepository.GetByCountryAsync"/>,
/// <see cref="IRuleDefinitionRepository.GetByKeyAsync"/>, the
/// <c>record.CorrelationId == correlationId</c> predicate inside
/// <see cref="IAuditSearchService.SearchAsync"/>, and both
/// <c>account.Username == username</c>/<c>account.TenantId == tenantId</c> inside
/// <see cref="IUserAccountRepository.GetByUsernameAsync"/>.</item>
/// <item>A predicate reaching into an owned-type navigation
/// (<see cref="IOutboxEntryRepository.GetDeadLetteredAsync"/>'s own
/// <c>entry.Envelope.TenantId</c> filter) or translating a client-side collection
/// into a SQL <c>IN</c> clause over a plain enum column
/// (<see cref="IRolePermissionGrantRepository.GetActiveGrantsForRolesAsync"/>'s own
/// <c>roles.Contains(grant.Role)</c>).</item>
/// </list>
///
/// Every one of the seven passes: each translates to the correct SQL, no
/// client-side evaluation fallback, no "could not be translated" exception,
/// confirmed against a real, disposable PostgreSQL 16 instance (see
/// <see cref="PostgresContainerFixture"/>), not the EF Core InMemory provider, per
/// this project's own testing standard ("An in-memory provider is not
/// acceptable... does not reproduce PostgreSQL behaviour for... LINQ translation").
/// No repository code changed as a result -- every comparison was already correct
/// as written; what changed is that it is now known to be correct rather than
/// merely assumed to be.
///
/// Deliberately narrow: this confirms the *mechanisms* generalize (the same
/// HasConversion/owned-navigation/Contains shapes any future repository will also
/// use, including whatever Sprint 4 adds), not full CRUD coverage of any of the
/// seven repositories -- see this project's own csproj header for why broader
/// repository/command/query coverage is HEP-33's scope, not this one.
/// </summary>
public sealed class RepositoryQueryTranslationTests : IClassFixture<PostgresContainerFixture>
{
    private readonly PostgresContainerFixture _fixture;

    public RepositoryQueryTranslationTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ConfigurationSettingRepository_GetByKeyAndScopeAsync_TranslatesKeyComparison()
    {
        var key = ConfigurationKey.Create("IntegrationTests.RepoQuery.ConfigKey").Value;
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
                changeSummary: "Repository query translation verification seed",
                createdByUserId: Guid.NewGuid(),
                nowUtc: DateTimeOffset.UtcNow).Value;

            await repository.AddAsync(setting, CancellationToken.None);
            await dbContext.SaveChangesAsync(CancellationToken.None);
        }

        // A fresh scope, and therefore a fresh HrisDbContext/change tracker, so this
        // read genuinely round-trips through Npgsql rather than being satisfied from
        // the write scope's own in-memory identity map. Every test below follows the
        // same write-scope/read-scope split for the same reason.
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
                CurrencyCode.Create("PHP").Value,
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

    [Fact]
    public async Task RuleDefinitionRepository_GetByKeyAsync_TranslatesKeyComparison()
    {
        var key = RuleKey.Create("IntegrationTests.RepoQuery.RuleKey").Value;

        using (var writeScope = _fixture.CreateScope())
        {
            var repository = writeScope.ServiceProvider.GetRequiredService<IRuleDefinitionRepository>();
            var dbContext = writeScope.ServiceProvider.GetRequiredService<HrisDbContext>();

            var condition = RuleCondition.Create("field", ComparisonOperator.Equals, "value").Value;
            var action = RuleActionDirective.Create("action-key").Value;
            var definition = RuleDefinition.Create(
                key,
                "category",
                [condition],
                LogicalOperator.All,
                [action],
                RulePriority.Normal,
                new UserAccountId(Guid.NewGuid()),
                DateTimeOffset.UtcNow).Value;

            await repository.AddAsync(definition, CancellationToken.None);
            await dbContext.SaveChangesAsync(CancellationToken.None);
        }

        using var readScope = _fixture.CreateScope();
        var readRepository = readScope.ServiceProvider.GetRequiredService<IRuleDefinitionRepository>();

        var found = await readRepository.GetByKeyAsync(key, CancellationToken.None);

        found.Should().NotBeNull();
        found!.Key.Should().Be(key);
    }

    [Fact]
    public async Task AuditSearchService_SearchAsync_TranslatesCorrelationIdComparison()
    {
        var correlationGuid = Guid.NewGuid();
        var correlationId = CorrelationId.Create(correlationGuid).Value;

        using (var writeScope = _fixture.CreateScope())
        {
            var repository = writeScope.ServiceProvider.GetRequiredService<IAuditRecordRepository>();
            var dbContext = writeScope.ServiceProvider.GetRequiredService<HrisDbContext>();

            var record = AuditRecord.Create(
                DateTimeOffset.UtcNow,
                actorId: null,
                AuditCategory.Business,
                action: "action",
                businessEntity: "entity",
                entityIdentifier: "id",
                sourceSystem: "source",
                AuditResult.Success,
                correlationId: correlationId).Value;

            await repository.AddAsync(record, CancellationToken.None);
            await dbContext.SaveChangesAsync(CancellationToken.None);
        }

        using var readScope = _fixture.CreateScope();
        var searchService = readScope.ServiceProvider.GetRequiredService<IAuditSearchService>();

        var results = await searchService.SearchAsync(
            new AuditSearchCriteria(CorrelationId: correlationGuid), pageNumber: 1, pageSize: 10, CancellationToken.None);

        results.Should().ContainSingle(record => record.CorrelationId!.Value == correlationGuid);
    }

    [Fact]
    public async Task UserAccountRepository_GetByUsernameAsync_TranslatesUsernameAndTenantIdComparison()
    {
        var tenantId = Guid.NewGuid();
        var username = Username.Create("repoquery.verification").Value;
        var email = EmailAddress.Create("repoquery.verification@example.com").Value;

        using (var writeScope = _fixture.CreateScope())
        {
            var repository = writeScope.ServiceProvider.GetRequiredService<IUserAccountRepository>();
            var dbContext = writeScope.ServiceProvider.GetRequiredService<HrisDbContext>();

            var account = UserAccount.Create(
                tenantId,
                username,
                email,
                displayName: null,
                IdentityType.Employee,
                AuthenticationProvider.Local(),
                DateTimeOffset.UtcNow).Value;

            await repository.AddAsync(account, CancellationToken.None);
            await dbContext.SaveChangesAsync(CancellationToken.None);
        }

        using var readScope = _fixture.CreateScope();
        var readRepository = readScope.ServiceProvider.GetRequiredService<IUserAccountRepository>();

        var found = await readRepository.GetByUsernameAsync(username, tenantId, CancellationToken.None);

        found.Should().NotBeNull();
        found!.Username.Should().Be(username);
        found.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public async Task OutboxEntryRepository_GetDeadLetteredAsync_TranslatesOwnedEnvelopeTenantIdComparison()
    {
        var tenantId = Guid.NewGuid();

        using (var writeScope = _fixture.CreateScope())
        {
            var repository = writeScope.ServiceProvider.GetRequiredService<IOutboxEntryRepository>();
            var dbContext = writeScope.ServiceProvider.GetRequiredService<HrisDbContext>();

            var domainEvent = new RepositoryQueryVerificationEvent(Guid.NewGuid(), DateTimeOffset.UtcNow);
            var envelope = EventEnvelope.Create(
                domainEvent,
                sourceModule: "IntegrationTests",
                EventCategory.DomainEvent,
                CorrelationId.NewId(),
                payload: "{}",
                tenantId).Value;

            var entry = OutboxEntry.Create(envelope, DateTimeOffset.UtcNow).Value;

            // maxAttempts: 1 -- the first failed attempt already meets the
            // AttemptCount >= maxAttempts threshold, so this entry lands directly in
            // DeadLettered, exactly the status GetDeadLetteredAsync filters on.
            entry.RecordFailedAttempt("forced failure for verification", DateTimeOffset.UtcNow, maxAttempts: 1);

            await repository.AddAsync(entry, CancellationToken.None);
            await dbContext.SaveChangesAsync(CancellationToken.None);
        }

        using var readScope = _fixture.CreateScope();
        var readRepository = readScope.ServiceProvider.GetRequiredService<IOutboxEntryRepository>();

        var results = await readRepository.GetDeadLetteredAsync(tenantId, maxResults: 10, CancellationToken.None);

        results.Should().ContainSingle(entry => entry.Envelope.TenantId == tenantId);
    }

    [Fact]
    public async Task RolePermissionGrantRepository_GetActiveGrantsForRolesAsync_TranslatesRolesContains()
    {
        var permission = PermissionKey.Create("IntegrationTests.RepoQuery.Resource", PermissionAction.Read).Value;

        using (var writeScope = _fixture.CreateScope())
        {
            var repository = writeScope.ServiceProvider.GetRequiredService<IRolePermissionGrantRepository>();
            var dbContext = writeScope.ServiceProvider.GetRequiredService<HrisDbContext>();

            var grant = RolePermissionGrant.Create(Role.HRManager, permission, DateTimeOffset.UtcNow).Value;

            await repository.AddAsync(grant, CancellationToken.None);
            await dbContext.SaveChangesAsync(CancellationToken.None);
        }

        using var readScope = _fixture.CreateScope();
        var readRepository = readScope.ServiceProvider.GetRequiredService<IRolePermissionGrantRepository>();

        var results = await readRepository.GetActiveGrantsForRolesAsync([Role.HRManager, Role.Employee], CancellationToken.None);

        results.Should().Contain(grant => grant.Role == Role.HRManager && grant.Permission == permission);
    }

    private sealed record RepositoryQueryVerificationEvent(Guid EventId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
}
