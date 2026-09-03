namespace Hris.Foundation.Search.Application.Dtos;

/// <summary>
/// The read-side shape <c>GlobalSearchQuery</c> returns -- results grouped by domain,
/// per search-framework.md's own <c>GlobalSearchQuery</c> subsection: "returns results
/// grouped by domain, security-trimmed against the caller's own existing per-domain
/// permissions before the response leaves the server."
/// </summary>
public sealed record GlobalSearchResultDto(
    Guid SearchExecutionId,
    string QueryText,
    int TotalResultCount,
    IReadOnlyList<SearchResultGroupDto> Groups);

public sealed record SearchResultGroupDto(
    string Domain,
    IReadOnlyList<SearchResultHitDto> Hits);

/// <summary>
/// <see cref="ContentSnippet"/> is a plain truncation of the matched document's own
/// searchable content, not true term highlighting -- search-framework.md's own Search
/// Highlighting bullet is named in Scope but not built this pass; no CTR or NFR this
/// document names requires it, matching this Sprint's own "build what the AI
/// Implementation Guidance actually names as non-negotiable" discipline every other
/// Sprint 4 framework's own remarks state for its own deferred stretch features.
/// </summary>
public sealed record SearchResultHitDto(
    Guid IndexedDocumentId,
    string SourceEntityId,
    string ContentSnippet,
    double Rank);
