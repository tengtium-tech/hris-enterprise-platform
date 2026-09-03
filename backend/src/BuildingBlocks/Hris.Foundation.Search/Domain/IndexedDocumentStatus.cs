namespace Hris.Foundation.Search.Domain;

/// <summary>
/// An <see cref="IndexedDocument"/>'s own lifecycle. Two states only -- this Sprint's
/// own build has no concrete consumer for a third "Stale, pending reindex" state (no
/// scheduled-reindexing background job exists yet, see <see cref="IndexedDocument"/>'s
/// own remarks), so it is not added speculatively. <see cref="Removed"/> is a soft
/// delete, excluded from default search results per <c>CTR-DAT-004</c>.
/// </summary>
public enum IndexedDocumentStatus
{
    Indexed = 0,
    Removed = 1,
}
