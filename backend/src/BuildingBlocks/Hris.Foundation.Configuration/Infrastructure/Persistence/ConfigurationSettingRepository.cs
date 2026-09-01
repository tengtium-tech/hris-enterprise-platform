using Hris.Foundation.Configuration.Domain;
using Hris.Infrastructure.Persistence;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hris.Foundation.Configuration.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IConfigurationSettingRepository"/>, per
/// docs/02-architecture/04-domain-driven-design/repositories.md's "Repository
/// interfaces belong in the Domain layer... Implementation in Infrastructure."
///
/// Every read here loads the full Aggregate (<c>.Include</c> the owned Versions
/// collection) rather than projecting -- aggregate-persistence.md's Loading Aggregates
/// section: "Repositories load complete Aggregates when business behavior is
/// required... Projection queries should be used when only read-only data is
/// required," and every caller of this repository (the command handlers in
/// Application/Commands/) needs to call a behavior method on the loaded Aggregate,
/// which is exactly the "business behavior required" case. <see cref="Application.Queries.GetConfigurationSettingByKeyAndScopeQueryHandler"/>
/// is the read-only caller, and it too goes through this same full-aggregate load
/// rather than a separate projection -- accepted here as the simpler choice for a
/// small, rarely-queried-in-bulk aggregate; revisit with a dedicated projection if a
/// future caller needs to list many settings at once without their full version
/// history.
/// </summary>
/// <remarks>
/// VERIFIED (HEP-38): every <c>setting.Key == key</c> predicate below compares a
/// <see cref="ConfigurationKey"/> Value Object mapped through <c>HasConversion</c>,
/// and <see cref="ValueObject"/> overloads <c>==</c> with a custom operator rather
/// than relying on default reference equality -- reasonable grounds to doubt EF
/// Core's SQL translator would accept that overload rather than throwing "could not
/// be translated," or silently falling back to client evaluation. Confirmed against
/// a real, disposable PostgreSQL 16 instance via Testcontainers, not the EF Core
/// InMemory provider (whose more permissive client-evaluation fallback would not
/// have proven anything about the real provider) --
/// <c>Hris.Infrastructure.IntegrationTests.ValueObjectComparisonTranslationTests.ConfigurationSettingRepository_GetByKeyAndScopeAsync_TranslatesKeyComparison</c>
/// inserts a real row and reads it back through exactly this predicate, in a fresh
/// <c>HrisDbContext</c>/change-tracker scope so the read genuinely round-trips
/// through Npgsql. Passes: the operator overload resolves to a normal SQL equality
/// predicate on the converted column, per
/// docs/02-architecture/05-data-architecture/dbcontext-design.md's own Testing
/// section ("PostgreSQL integration tests... Avoid relying solely on the EF Core
/// InMemory provider").
/// </remarks>
internal sealed class ConfigurationSettingRepository : IConfigurationSettingRepository
{
    private readonly HrisDbContext _dbContext;

    public ConfigurationSettingRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public Task<ConfigurationSetting?> GetByIdAsync(ConfigurationId id, CancellationToken cancellationToken) =>
        _dbContext.Set<ConfigurationSetting>()
            .FirstOrDefaultAsync(setting => setting.Id == id, cancellationToken);

    public Task<ConfigurationSetting?> GetByKeyAndScopeAsync(
        ConfigurationKey key,
        ConfigurationScope scope,
        CancellationToken cancellationToken) =>
        _dbContext.Set<ConfigurationSetting>()
            .FirstOrDefaultAsync(
                setting => setting.Key == key && setting.Scope.Level == scope.Level && setting.Scope.ScopeId == scope.ScopeId,
                cancellationToken);

    public async Task<IReadOnlyList<ConfigurationSetting>> FindByKeyAcrossScopesAsync(
        ConfigurationKey key,
        IReadOnlyCollection<ConfigurationScope> candidateScopes,
        CancellationToken cancellationToken)
    {
        // EF Core cannot translate "scope is one of this in-memory list of owned-type
        // value objects" directly into SQL; the (Level, ScopeId) pairs are extracted
        // client-side first and compared as primitives, which EF Core's LINQ
        // translator does support (Contains over a materialized tuple list).
        var pairs = candidateScopes.Select(s => (s.Level, s.ScopeId)).ToList();

        var candidates = await _dbContext.Set<ConfigurationSetting>()
            .Where(setting => setting.Key == key)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return candidates
            .Where(setting => pairs.Contains((setting.Scope.Level, setting.Scope.ScopeId)))
            .ToList();
    }

    public Task<bool> ExistsAsync(ConfigurationKey key, ConfigurationScope scope, CancellationToken cancellationToken) =>
        _dbContext.Set<ConfigurationSetting>()
            .AnyAsync(
                setting => setting.Key == key && setting.Scope.Level == scope.Level && setting.Scope.ScopeId == scope.ScopeId,
                cancellationToken);

    public async Task AddAsync(ConfigurationSetting setting, CancellationToken cancellationToken) =>
        await _dbContext.Set<ConfigurationSetting>()
            .AddAsync(setting, cancellationToken)
            .ConfigureAwait(false);
}
