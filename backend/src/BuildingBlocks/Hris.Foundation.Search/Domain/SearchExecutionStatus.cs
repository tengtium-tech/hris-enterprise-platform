namespace Hris.Foundation.Search.Domain;

/// <summary>
/// A <see cref="SearchExecution"/>'s own lifecycle -- the exact triad
/// search-framework.md's own Domain Events section names for it (<c>SearchRequested</c>,
/// <c>SearchCompleted</c>, <c>SearchFailed</c>).
/// </summary>
public enum SearchExecutionStatus
{
    Requested = 0,
    Completed = 1,
    Failed = 2,
}
