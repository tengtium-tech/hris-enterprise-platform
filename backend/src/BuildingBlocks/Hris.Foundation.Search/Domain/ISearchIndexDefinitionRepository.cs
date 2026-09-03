namespace Hris.Foundation.Search.Domain;

/// <summary>
/// Repository contract for the <see cref="SearchIndexDefinition"/> Aggregate Root, per
/// repositories.md's "interface in the Domain layer... implementation in
/// Infrastructure" split.
/// </summary>
public interface ISearchIndexDefinitionRepository
{
    Task<SearchIndexDefinition?> GetByIdAsync(SearchIndexDefinitionId id, CancellationToken cancellationToken);

    Task<SearchIndexDefinition?> GetByEntityTypeAsync(SearchEntityType entityType, CancellationToken cancellationToken);

    Task<bool> ExistsByEntityTypeAsync(SearchEntityType entityType, CancellationToken cancellationToken);

    Task AddAsync(SearchIndexDefinition definition, CancellationToken cancellationToken);
}
