using Hris.Foundation.Search.Domain;
using Hris.Infrastructure.Persistence;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hris.Foundation.Search.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="ISearchExecutionRepository"/>, per
/// repositories.md's "interface in the Domain layer... implementation in
/// Infrastructure" split.
/// </summary>
internal sealed class SearchExecutionRepository : ISearchExecutionRepository
{
    private readonly HrisDbContext _dbContext;

    public SearchExecutionRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public Task<SearchExecution?> GetByIdAsync(SearchExecutionId id, CancellationToken cancellationToken) =>
        _dbContext.Set<SearchExecution>().FirstOrDefaultAsync(execution => execution.Id == id, cancellationToken);

    public async Task AddAsync(SearchExecution execution, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(execution, nameof(execution));
        await _dbContext.Set<SearchExecution>().AddAsync(execution, cancellationToken).ConfigureAwait(false);
    }
}
