using Hris.Application.Abstractions;
using Hris.Foundation.Search.Application.Dtos;
using Hris.Foundation.Search.Application.Mapping;
using Hris.Foundation.Search.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Search.Application.Queries;

/// <summary>
/// search-framework.md's own Search Suggestions section ("Auto Complete... Popular
/// Searches... Recent Searches... Saved Searches. Suggestions should respect user
/// permissions"). This Sprint's own build surfaces the one concrete, buildable slice --
/// the caller's own saved searches -- since Auto Complete/Popular/Recent Searches each
/// need a query-usage history projection no aggregate this Sprint tracks (see
/// <see cref="SavedSearch"/>'s own remarks for why <see cref="SearchExecution"/> is not
/// stretched to cover this instead: it is population-scale, not owner-scoped, the wrong
/// shape for "this specific user's own recent activity").
///
/// Not a pure read: each returned <see cref="SavedSearch"/> has
/// <see cref="SavedSearch.RecordSuggested"/> called on it, raising
/// <see cref="SearchSuggestionGenerated"/> -- see that method's own remarks for why a
/// bounded, top-N suggestion list is not the same per-record-event concern
/// <see cref="IndexedDocument"/>'s own remarks raise at population scale.
/// </summary>
public sealed record GetSearchSuggestionsQuery(Guid TenantId, Guid OwnerUserId) : IQuery<Result<IReadOnlyList<SavedSearchDto>>>;

internal sealed class GetSearchSuggestionsQueryHandler : IRequestHandler<GetSearchSuggestionsQuery, Result<IReadOnlyList<SavedSearchDto>>>
{
    private const int _maxSuggestions = 5;

    private readonly ISavedSearchRepository _repository;
    private readonly TimeProvider _timeProvider;

    public GetSearchSuggestionsQueryHandler(ISavedSearchRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<IReadOnlyList<SavedSearchDto>>> Handle(GetSearchSuggestionsQuery request, CancellationToken cancellationToken)
    {
        var savedSearches = await _repository
            .ListByOwnerAsync(request.TenantId, request.OwnerUserId, _maxSuggestions, cancellationToken)
            .ConfigureAwait(false);

        var nowUtc = _timeProvider.GetUtcNow();
        foreach (var savedSearch in savedSearches)
        {
            savedSearch.RecordSuggested(nowUtc);
        }

        IReadOnlyList<SavedSearchDto> dtos = savedSearches.Select(SearchMapper.ToDto).ToList();
        return Result.Success(dtos);
    }
}
