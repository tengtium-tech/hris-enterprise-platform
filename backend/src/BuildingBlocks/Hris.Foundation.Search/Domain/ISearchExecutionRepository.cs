namespace Hris.Foundation.Search.Domain;

/// <summary>
/// Repository contract for the <see cref="SearchExecution"/> Aggregate Root, per
/// repositories.md's "interface in the Domain layer... implementation in
/// Infrastructure" split.
/// </summary>
public interface ISearchExecutionRepository
{
    Task<SearchExecution?> GetByIdAsync(SearchExecutionId id, CancellationToken cancellationToken);

    Task AddAsync(SearchExecution execution, CancellationToken cancellationToken);
}
