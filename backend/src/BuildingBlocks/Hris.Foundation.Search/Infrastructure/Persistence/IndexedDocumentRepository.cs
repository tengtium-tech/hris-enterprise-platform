using Hris.Foundation.Search.Domain;
using Hris.Infrastructure.Persistence;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;
using Npgsql;

namespace Hris.Foundation.Search.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IIndexedDocumentRepository"/>, per
/// repositories.md's "interface in the Domain layer... implementation in
/// Infrastructure" split.
/// </summary>
internal sealed class IndexedDocumentRepository : IIndexedDocumentRepository
{
    private readonly HrisDbContext _dbContext;

    public IndexedDocumentRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public Task<IndexedDocument?> GetByIdAsync(IndexedDocumentId id, CancellationToken cancellationToken) =>
        _dbContext.Set<IndexedDocument>().FirstOrDefaultAsync(document => document.Id == id, cancellationToken);

    public Task<IndexedDocument?> FindBySourceAsync(
        SearchEntityType sourceEntityType, string sourceEntityId, Guid tenantId, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(sourceEntityType, nameof(sourceEntityType));
        Guard.AgainstNullOrWhiteSpace(sourceEntityId, nameof(sourceEntityId));

        return _dbContext.Set<IndexedDocument>().FirstOrDefaultAsync(
            document => document.TenantId == tenantId
                && document.SourceEntityType == sourceEntityType
                && document.SourceEntityId == sourceEntityId,
            cancellationToken);
    }

    public async Task AddAsync(IndexedDocument document, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(document, nameof(document));
        await _dbContext.Set<IndexedDocument>().AddAsync(document, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// A raw SQL query against PostgreSQL's own native full-text search -- see
    /// <see cref="IIndexedDocumentRepository.SearchAsync"/>'s own remarks for why a LINQ
    /// predicate cannot provide ranked relevance matching the way this does. Materializes
    /// into a private, all-primitive row type first, then reconstructs
    /// <see cref="IndexedDocumentSearchHit"/> (whose own properties include
    /// <see cref="IndexedDocumentId"/> and <see cref="SearchEntityType"/>, neither a
    /// primitive) in managed code -- EF Core's own ad-hoc <c>SqlQueryRaw&lt;T&gt;</c>
    /// column-to-property mapping for a type with no entity configuration of its own
    /// only supports primitive-shaped properties, the identical reason
    /// <c>IncrementAndGetNextSequenceValueAsync</c>'s own precedent targets a bare
    /// <c>long</c> rather than a richer shape directly.
    ///
    /// <paramref name="tenantId"/>, the excluded-by-default <see cref="IndexedDocumentStatus.Removed"/>
    /// status, <paramref name="domainFilter"/>, and <paramref name="callerScopeTokens"/>
    /// are every one applied inside this same <c>WHERE</c> clause -- never as a
    /// client-side post-filter over an unfiltered fetch -- so the tenant-isolation and
    /// security-trimming guarantees the AI Implementation Guidance names
    /// (<c>CTR-ISO-001</c>, "must not reveal the existence of records the user cannot
    /// access") hold even if a future caller here forgets to re-check them afterward.
    ///
    /// The SQL's own column aliases are written in snake_case
    /// (<c>indexed_document_id</c>, <c>rank</c>), not <see cref="SqlSearchHitRow"/>'s own
    /// PascalCase property names -- <c>SqlQueryRaw&lt;T&gt;</c>'s own ad-hoc
    /// column-to-property mapping runs each property name through this same
    /// <see cref="HrisDbContext"/>'s configured snake_case naming convention before
    /// matching it against the result set's own column names, the same transform every
    /// mapped entity's own columns already go through -- an aliased column literally
    /// named <c>IndexedDocumentId</c> fails with "The required column
    /// 'indexed_document_id' was not present," found empirically the same way this
    /// codebase's other EF Core pitfalls were, by a real query against a real database,
    /// not assumed from the SQL shape alone.
    /// </summary>
    public async Task<IReadOnlyList<IndexedDocumentSearchHit>> SearchAsync(
        Guid tenantId,
        string queryText,
        SearchEntityType? domainFilter,
        IReadOnlyCollection<string> callerScopeTokens,
        int maxResults,
        CancellationToken cancellationToken)
    {
        Guard.AgainstDefault(tenantId, nameof(tenantId));
        Guard.AgainstNullOrWhiteSpace(queryText, nameof(queryText));
        Guard.AgainstNull(callerScopeTokens, nameof(callerScopeTokens));

        var tenantIdParameter = new NpgsqlParameter("tenantId", tenantId);
        var queryTextParameter = new NpgsqlParameter("queryText", queryText);
        var domainFilterParameter = new NpgsqlParameter("domainFilter", NpgsqlDbType.Text)
        {
            Value = (object?)domainFilter?.Value ?? DBNull.Value,
        };
        var scopeTokensParameter = new NpgsqlParameter("callerScopeTokens", NpgsqlDbType.Array | NpgsqlDbType.Text)
        {
            Value = callerScopeTokens.ToArray(),
        };
        var removedStatusParameter = new NpgsqlParameter("removedStatus", (int)IndexedDocumentStatus.Removed);
        var maxResultsParameter = new NpgsqlParameter("maxResults", maxResults);

        var rows = await _dbContext.Database
            .SqlQueryRaw<SqlSearchHitRow>(
                """
                SELECT id AS indexed_document_id,
                       source_entity_type,
                       source_entity_id,
                       searchable_content,
                       ts_rank(to_tsvector('english', searchable_content), plainto_tsquery('english', @queryText)) AS rank
                FROM indexed_document
                WHERE tenant_id = @tenantId
                  AND status <> @removedStatus
                  AND (@domainFilter::text IS NULL OR source_entity_type = @domainFilter)
                  AND (security_scope_token IS NULL OR security_scope_token = ANY(@callerScopeTokens))
                  AND to_tsvector('english', searchable_content) @@ plainto_tsquery('english', @queryText)
                ORDER BY ts_rank(to_tsvector('english', searchable_content), plainto_tsquery('english', @queryText)) DESC
                LIMIT @maxResults
                """,
                tenantIdParameter, queryTextParameter, domainFilterParameter, scopeTokensParameter, removedStatusParameter, maxResultsParameter)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows
            .Select(row => new IndexedDocumentSearchHit(
                new IndexedDocumentId(row.IndexedDocumentId),
                SearchEntityType.Create(row.SourceEntityType).Value,
                row.SourceEntityId,
                row.SearchableContent,
                row.Rank))
            .ToList();
    }

    private sealed record SqlSearchHitRow(Guid IndexedDocumentId, string SourceEntityType, string SourceEntityId, string SearchableContent, double Rank);
}
