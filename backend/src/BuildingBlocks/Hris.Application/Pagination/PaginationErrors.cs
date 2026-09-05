using Hris.SharedKernel;

namespace Hris.Application.Pagination;

/// <summary>
/// This shared type's own reusable error catalog, per error-pattern.md's "Error
/// Catalog" section -- every future query's own <see cref="PageRequest"/> validation
/// returns one of these three, never a bespoke pagination error per module.
/// </summary>
public static class PaginationErrors
{
    public static readonly Error PageMustBePositive = new(
        "Pagination.PageMustBePositive",
        "Page must be 1 or greater.",
        ErrorCategory.Validation);

    public static readonly Error PageSizeMustBePositive = new(
        "Pagination.PageSizeMustBePositive",
        "Page size must be 1 or greater.",
        ErrorCategory.Validation);

    public static readonly Error PageSizeExceedsMaximum = new(
        "Pagination.PageSizeExceedsMaximum",
        "The requested page size exceeds this endpoint's own maximum.",
        ErrorCategory.Validation);
}
