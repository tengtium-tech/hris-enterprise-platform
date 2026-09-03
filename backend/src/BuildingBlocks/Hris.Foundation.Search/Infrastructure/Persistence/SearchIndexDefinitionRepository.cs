using Hris.Foundation.Search.Domain;
using Hris.Infrastructure.Persistence;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hris.Foundation.Search.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="ISearchIndexDefinitionRepository"/>, per
/// repositories.md's "interface in the Domain layer... implementation in
/// Infrastructure" split. No <c>UpdateAsync</c>: an aggregate loaded through
/// <see cref="GetByIdAsync"/> is already tracked by this same <see cref="HrisDbContext"/>,
/// so the caller's own <c>TransactionBehavior</c> persists any mutation via change
/// tracking alone.
/// </summary>
internal sealed class SearchIndexDefinitionRepository : ISearchIndexDefinitionRepository
{
    private readonly HrisDbContext _dbContext;

    public SearchIndexDefinitionRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public Task<SearchIndexDefinition?> GetByIdAsync(SearchIndexDefinitionId id, CancellationToken cancellationToken) =>
        _dbContext.Set<SearchIndexDefinition>().FirstOrDefaultAsync(definition => definition.Id == id, cancellationToken);

    public Task<SearchIndexDefinition?> GetByEntityTypeAsync(SearchEntityType entityType, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(entityType, nameof(entityType));

        return _dbContext.Set<SearchIndexDefinition>().FirstOrDefaultAsync(definition => definition.EntityType == entityType, cancellationToken);
    }

    public Task<bool> ExistsByEntityTypeAsync(SearchEntityType entityType, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(entityType, nameof(entityType));

        return _dbContext.Set<SearchIndexDefinition>().AnyAsync(definition => definition.EntityType == entityType, cancellationToken);
    }

    public async Task AddAsync(SearchIndexDefinition definition, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(definition, nameof(definition));
        await _dbContext.Set<SearchIndexDefinition>().AddAsync(definition, cancellationToken).ConfigureAwait(false);
    }
}
