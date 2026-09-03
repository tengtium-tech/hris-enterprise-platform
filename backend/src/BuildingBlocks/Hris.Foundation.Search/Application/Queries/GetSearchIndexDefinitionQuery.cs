using Hris.Application.Abstractions;
using Hris.Foundation.Search.Application.Dtos;
using Hris.Foundation.Search.Application.Mapping;
using Hris.Foundation.Search.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Search.Application.Queries;

/// <summary>
/// Reads one search index definition back by its own natural key -- the entity type
/// name a caller registering or indexing content actually has in hand, matching
/// <c>GetNumberSeriesQuery</c>'s own identical by-natural-key shape.
/// </summary>
public sealed record GetSearchIndexDefinitionQuery(string EntityType) : IQuery<Result<SearchIndexDefinitionDto>>;

internal sealed class GetSearchIndexDefinitionQueryHandler : IRequestHandler<GetSearchIndexDefinitionQuery, Result<SearchIndexDefinitionDto>>
{
    private readonly ISearchIndexDefinitionRepository _repository;

    public GetSearchIndexDefinitionQueryHandler(ISearchIndexDefinitionRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<SearchIndexDefinitionDto>> Handle(GetSearchIndexDefinitionQuery request, CancellationToken cancellationToken)
    {
        var entityTypeResult = SearchEntityType.Create(request.EntityType);
        if (entityTypeResult.IsFailure)
        {
            return Result.Failure<SearchIndexDefinitionDto>(entityTypeResult.Error);
        }

        var definition = await _repository.GetByEntityTypeAsync(entityTypeResult.Value, cancellationToken).ConfigureAwait(false);

        return definition is null
            ? Result.Failure<SearchIndexDefinitionDto>(SearchErrors.SearchIndexDefinitionNotFound)
            : Result.Success(SearchMapper.ToDto(definition));
    }
}
