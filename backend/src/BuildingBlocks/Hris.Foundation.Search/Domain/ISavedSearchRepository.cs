namespace Hris.Foundation.Search.Domain;

/// <summary>
/// Repository contract for the <see cref="SavedSearch"/> Aggregate Root, per
/// repositories.md's "interface in the Domain layer... implementation in
/// Infrastructure" split. <see cref="ListByOwnerAsync"/> takes both
/// <paramref name="tenantId"/> and <paramref name="ownerUserId"/> as mandatory
/// parameters -- see <see cref="SavedSearch"/>'s own remarks for why that shape is the
/// structural guarantee behind "suggestions should respect user permissions," not an
/// optional filter a caller could omit.
/// </summary>
public interface ISavedSearchRepository
{
    Task<SavedSearch?> GetByIdAsync(SavedSearchId id, CancellationToken cancellationToken);

    Task<IReadOnlyList<SavedSearch>> ListByOwnerAsync(
        Guid tenantId, Guid ownerUserId, int maxResults, CancellationToken cancellationToken);

    Task AddAsync(SavedSearch savedSearch, CancellationToken cancellationToken);

    Task DeleteAsync(SavedSearch savedSearch, CancellationToken cancellationToken);
}
