using Hris.Foundation.Search.Domain;
using Hris.Infrastructure.Persistence;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hris.Foundation.Search.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="ISavedSearchRepository"/>, per
/// repositories.md's "interface in the Domain layer... implementation in
/// Infrastructure" split.
/// </summary>
internal sealed class SavedSearchRepository : ISavedSearchRepository
{
    private readonly HrisDbContext _dbContext;

    public SavedSearchRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public Task<SavedSearch?> GetByIdAsync(SavedSearchId id, CancellationToken cancellationToken) =>
        _dbContext.Set<SavedSearch>().FirstOrDefaultAsync(savedSearch => savedSearch.Id == id, cancellationToken);

    public async Task<IReadOnlyList<SavedSearch>> ListByOwnerAsync(
        Guid tenantId, Guid ownerUserId, int maxResults, CancellationToken cancellationToken) =>
        await _dbContext.Set<SavedSearch>()
            .Where(savedSearch => savedSearch.TenantId == tenantId && savedSearch.OwnerUserId == ownerUserId)
            .OrderByDescending(savedSearch => savedSearch.CreatedAtUtc)
            .Take(maxResults)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task AddAsync(SavedSearch savedSearch, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(savedSearch, nameof(savedSearch));
        await _dbContext.Set<SavedSearch>().AddAsync(savedSearch, cancellationToken).ConfigureAwait(false);
    }

    public Task DeleteAsync(SavedSearch savedSearch, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(savedSearch, nameof(savedSearch));
        _dbContext.Set<SavedSearch>().Remove(savedSearch);
        return Task.CompletedTask;
    }
}
