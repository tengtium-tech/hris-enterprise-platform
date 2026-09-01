using Hris.Foundation.RulesEngine.Domain;
using Hris.Infrastructure.Persistence;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hris.Foundation.RulesEngine.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IRuleDefinitionRepository"/>, per
/// repositories.md's "interface in the Domain layer... implementation in
/// Infrastructure" split -- the identical shape <c>ConfigurationSettingRepository</c>
/// already establishes for the sibling lifecycle this framework's own Domain layer
/// mirrors.
/// </summary>
/// <remarks>
/// VERIFIED: the <c>definition.Key == key</c> comparison below compares a converted
/// <see cref="RuleKey"/> Value Object to a constant -- the identical shape
/// <c>ConfigurationSettingRepository</c>'s own remarks already confirmed (HEP-38).
/// Confirmed here too, against a real, disposable PostgreSQL 16 instance via
/// Testcontainers -- see
/// <c>Hris.Infrastructure.IntegrationTests.RepositoryQueryTranslationTests.RuleDefinitionRepository_GetByKeyAsync_TranslatesKeyComparison</c>.
/// Passes: no fix needed.
/// </remarks>
internal sealed class RuleDefinitionRepository : IRuleDefinitionRepository
{
    private readonly HrisDbContext _dbContext;

    public RuleDefinitionRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public Task<RuleDefinition?> GetByIdAsync(RuleDefinitionId id, CancellationToken cancellationToken) =>
        _dbContext.Set<RuleDefinition>()
            .FirstOrDefaultAsync(definition => definition.Id == id, cancellationToken);

    public Task<RuleDefinition?> GetByKeyAsync(RuleKey key, CancellationToken cancellationToken) =>
        _dbContext.Set<RuleDefinition>()
            .FirstOrDefaultAsync(definition => definition.Key == key, cancellationToken);

    public async Task AddAsync(RuleDefinition definition, CancellationToken cancellationToken) =>
        await _dbContext.Set<RuleDefinition>()
            .AddAsync(definition, cancellationToken)
            .ConfigureAwait(false);
}
