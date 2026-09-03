using Hris.Application.Abstractions;
using Hris.Foundation.Search.Application.Dtos;
using Hris.Foundation.Search.Application.Mapping;
using Hris.Foundation.Search.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Search.Application.Queries;

/// <summary>
/// Lists one user's own saved searches -- always scoped to the requesting caller's own
/// <see cref="SavedSearchDto"/> rows, per <see cref="ISavedSearchRepository.ListByOwnerAsync"/>'s
/// own remarks on "suggestions should respect user permissions."
/// </summary>
public sealed record ListSavedSearchesQuery(Guid TenantId, Guid OwnerUserId) : IQuery<Result<IReadOnlyList<SavedSearchDto>>>;

internal sealed class ListSavedSearchesQueryHandler : IRequestHandler<ListSavedSearchesQuery, Result<IReadOnlyList<SavedSearchDto>>>
{
    private const int _maxResults = 100;

    private readonly ISavedSearchRepository _repository;

    public ListSavedSearchesQueryHandler(ISavedSearchRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<IReadOnlyList<SavedSearchDto>>> Handle(ListSavedSearchesQuery request, CancellationToken cancellationToken)
    {
        var savedSearches = await _repository
            .ListByOwnerAsync(request.TenantId, request.OwnerUserId, _maxResults, cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<SavedSearchDto> dtos = savedSearches.Select(SearchMapper.ToDto).ToList();
        return Result.Success(dtos);
    }
}
