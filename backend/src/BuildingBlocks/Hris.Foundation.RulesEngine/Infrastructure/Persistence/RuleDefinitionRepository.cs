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
/// UNVERIFIED (backend/README.md, "What hasn't been verified yet"): both
/// <c>definition.Key == key</c> comparisons below compare a converted
/// <see cref="RuleKey"/> Value Object to a constant -- the identical unverified
/// translation concern <c>ConfigurationSettingRepository</c>'s own remarks state for
/// its own Value Object comparisons, not yet run against a real PostgreSQL instance in
/// this sandbox.
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
