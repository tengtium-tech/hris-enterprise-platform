namespace Hris.Foundation.Search.Domain;

/// <summary>
/// Repository contract for the <see cref="IndexedDocument"/> Aggregate Root, per
/// repositories.md's "interface in the Domain layer... implementation in
/// Infrastructure" split. Every read method below takes <c>tenantId</c> as a mandatory
/// parameter -- see <see cref="IndexedDocument"/>'s own remarks for why this is
/// deliberate structure, not an incidental filter, in this specific framework.
/// </summary>
public interface IIndexedDocumentRepository
{
    Task<IndexedDocument?> GetByIdAsync(IndexedDocumentId id, CancellationToken cancellationToken);

    /// <summary>
    /// Finds the existing indexed document for one source record, if any -- how
    /// <c>IndexDocumentCommand</c>'s own handler decides between
    /// <see cref="IndexedDocument.Index"/> (first time this source record is indexed)
    /// and <see cref="IndexedDocument.UpdateContent"/> (every subsequent time), giving
    /// "Event-Driven Index Updates" idempotent create-or-update behavior from a single
    /// caller-facing command.
    /// </summary>
    Task<IndexedDocument?> FindBySourceAsync(
        SearchEntityType sourceEntityType, string sourceEntityId, Guid tenantId, CancellationToken cancellationToken);

    Task AddAsync(IndexedDocument document, CancellationToken cancellationToken);

    /// <summary>
    /// The single genuine full-text search operation this framework provides,
    /// implementing <c>GlobalSearchQuery</c>. Raw SQL against PostgreSQL's own native
    /// full-text search (<c>to_tsvector</c>/<c>plainto_tsquery</c>/<c>ts_rank</c>),
    /// never a LINQ predicate -- the identical "a genuine correctness/performance
    /// property EF Core's own LINQ translation cannot provide" reasoning
    /// <c>INumberSeriesRepository.IncrementAndGetNextSequenceValueAsync</c>'s own
    /// remarks state for its own raw SQL, applied here to ranked relevance matching
    /// instead of atomic increment. No ADR in this repository names an external search
    /// engine (Elasticsearch or otherwise) as an accepted technology choice, so this
    /// stays inside PostgreSQL per ADR-0001 rather than introducing one unilaterally.
    ///
    /// <paramref name="tenantId"/> and <paramref name="domainFilter"/> are applied
    /// inside the SQL <c>WHERE</c> clause itself, never as a client-side post-filter --
    /// the same "security-trimmed... before the response leaves the server" requirement
    /// search-framework.md's own <c>GlobalSearchQuery</c> subsection states for
    /// authorization, applied here to tenant isolation as well
    /// (<c>CTR-ISO-001</c>). <see cref="IndexedDocumentStatus.Removed"/> rows are
    /// excluded unconditionally (<c>CTR-DAT-004</c>).
    /// </summary>
    Task<IReadOnlyList<IndexedDocumentSearchHit>> SearchAsync(
        Guid tenantId,
        string queryText,
        SearchEntityType? domainFilter,
        IReadOnlyCollection<string> callerScopeTokens,
        int maxResults,
        CancellationToken cancellationToken);
}
