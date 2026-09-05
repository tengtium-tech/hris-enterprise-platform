using Hris.SharedKernel;

namespace Hris.Application.Pagination;

/// <summary>
/// api-standards.md's own Pagination, Filtering, and Sorting section: "Page number ...
/// page ... 1-indexed. Omitted defaults to 1" and "Page size ... pageSize ... Bounded
/// maximum per endpoint ... A request exceeding the maximum is rejected with 400,
/// never silently capped." Every future query taking `page`/`pageSize` parameters
/// constructs one of these and calls <see cref="Validate"/> before doing anything
/// else with it -- the query handler never re-derives this check, the same "one
/// evaluation point" discipline this platform already applies to authorization and
/// entitlement.
/// </summary>
public sealed record PageRequest(int Page, int PageSize)
{
    public const int DefaultPage = 1;

    /// <summary>
    /// Rejects a non-positive page, a non-positive page size, or a page size
    /// exceeding <paramref name="maxPageSize"/> -- silently capping "returns partial
    /// data reported as complete, and the caller acts on a list they believe is
    /// whole" (api-standards.md's own reasoning, elevated from
    /// administration.md/workflow.md to the platform standard).
    /// </summary>
    public Result Validate(int maxPageSize)
    {
        if (Page < 1)
        {
            return Result.Failure(PaginationErrors.PageMustBePositive);
        }

        if (PageSize < 1)
        {
            return Result.Failure(PaginationErrors.PageSizeMustBePositive);
        }

        return PageSize > maxPageSize
            ? Result.Failure(PaginationErrors.PageSizeExceedsMaximum)
            : Result.Success();
    }
}
