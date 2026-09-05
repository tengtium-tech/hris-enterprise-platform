namespace Hris.Application.Pagination;

/// <summary>
/// api-standards.md's own Pagination section: "A response's pagination metadata
/// (page, pageSize, totalCount, totalPages) is carried in a consistent envelope
/// alongside the result array." <see cref="TotalPages"/> is computed, never a fourth
/// independently-settable field, so it cannot drift out of sync with
/// <see cref="TotalCount"/>/<see cref="PageSize"/>.
///
/// Callers are expected to have already applied permission and scope filtering
/// before constructing one of these (`CTR-AUT-009`: "permission filtering precedes
/// pagination") -- this type carries the already-filtered page, it does not itself
/// filter anything.
/// </summary>
public sealed record PagedResult<T>(IReadOnlyCollection<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
