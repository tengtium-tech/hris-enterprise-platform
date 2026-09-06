using Hris.Foundation.DocumentManagement.Domain;
using Hris.Infrastructure.Persistence;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hris.Foundation.DocumentManagement.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IDocumentRepository"/>, per repositories.md's
/// "interface in the Domain layer... implementation in Infrastructure" split. No
/// <c>UpdateAsync</c>: an aggregate loaded through <see cref="GetByIdAsync"/> is
/// already tracked by this same <see cref="HrisDbContext"/>, so the caller's own
/// <c>TransactionBehavior</c> persists any mutation via change tracking alone.
/// </summary>
internal sealed class DocumentRepository : IDocumentRepository
{
    private readonly HrisDbContext _dbContext;

    public DocumentRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public Task<Document?> GetByIdAsync(DocumentId id, CancellationToken cancellationToken) =>
        _dbContext.Set<Document>().FirstOrDefaultAsync(document => document.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<Document> Items, int TotalCount)> SearchAsync(
        Guid tenantId,
        string? category,
        DocumentClassification? classification,
        string? titleContains,
        IReadOnlyList<string>? tags,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Set<Document>().Where(document => document.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(document => document.Category == category);
        }

        if (classification is not null)
        {
            query = query.Where(document => document.Classification == classification);
        }

        if (!string.IsNullOrWhiteSpace(titleContains))
        {
            query = query.Where(document => EF.Functions.ILike(document.Title, $"%{titleContains}%"));
        }

        query = query.OrderByDescending(document => document.CreatedAtUtc);

        // Document.Tags is mapped through a value converter (DocumentConfiguration's
        // own remarks) to a single delimited column, so a per-tag containment check
        // cannot be translated into SQL the way a native array column's own "contains"
        // operator could -- EF Core would throw at query-execution time if this were
        // expressed as a further .Where() on the query above. Tag filtering is
        // therefore applied client-side, after every other filter has already reduced
        // the candidate set in SQL; this trades a wider in-memory materialization for
        // correctness now, an accepted gap this Sprint's own build does not need a
        // native array/JSON column redesign to close.
        if (tags is { Count: > 0 })
        {
            var candidates = await query.ToListAsync(cancellationToken).ConfigureAwait(false);
            var matching = candidates.Where(document => tags.All(tag => document.Tags.Contains(tag))).ToList();

            return (matching.Skip(skip).Take(take).ToList(), matching.Count);
        }

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        var items = await query
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (items, totalCount);
    }

    public async Task AddAsync(Document document, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(document, nameof(document));
        await _dbContext.Set<Document>().AddAsync(document, cancellationToken).ConfigureAwait(false);
    }
}
