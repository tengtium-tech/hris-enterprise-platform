namespace Hris.Foundation.Search.Domain;

/// <summary>
/// One row of <see cref="IIndexedDocumentRepository.SearchAsync"/>'s own result --
/// a read-only projection, not the <see cref="IndexedDocument"/> Aggregate Root itself,
/// since a full-text search result never needs to mutate the document it matched.
/// </summary>
public sealed record IndexedDocumentSearchHit(
    IndexedDocumentId IndexedDocumentId,
    SearchEntityType SourceEntityType,
    string SourceEntityId,
    string SearchableContent,
    double Rank);
