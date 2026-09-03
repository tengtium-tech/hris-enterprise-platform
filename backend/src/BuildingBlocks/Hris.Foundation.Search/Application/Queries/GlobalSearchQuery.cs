using Hris.Application.Abstractions;
using Hris.Foundation.Search.Application.Dtos;
using Hris.Foundation.Search.Application.Mapping;
using Hris.Foundation.Search.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Search.Application.Queries;

/// <summary>
/// The single genuine cross-module search route search-framework.md's own
/// <c>GlobalSearchQuery</c> subsection names -- <c>GET /api/v1/search</c>, catalogued
/// as an explicitly-labeled Foundation exception in api-catalog.md Section 22.
///
/// Not a pure read despite being a Query: it also creates and persists a
/// <see cref="SearchExecution"/> to raise the real domain events
/// (<see cref="SearchRequested"/>/<see cref="SearchCompleted"/>/<see cref="SearchFailed"/>)
/// search-framework.md's own Domain Events section names -- the identical, deliberate
/// CQRS-blurring this framework's own spec forces, since a log line would not satisfy
/// "raise a domain event," and a genuinely separate Command the caller must also invoke
/// would let a caller execute a search that is never recorded. Documented here rather
/// than glossed over, per this repository's own house style for a non-obvious
/// departure.
///
/// <paramref name="TenantId"/> and <paramref name="CallerScopeTokens"/> are supplied
/// by the caller (the API layer, which has already resolved the authenticated
/// requester's own tenant and permission scopes) -- this handler applies them, it does
/// not resolve them, matching <see cref="IndexedDocument"/>'s own remarks on why
/// Authorization Framework's concrete evaluation stays deferred while the structural
/// hook for it (mandatory scope-token parameters on every query) is built now.
/// </summary>
public sealed record GlobalSearchQuery(
    Guid TenantId,
    Guid RequestedByUserId,
    string QueryText,
    string? DomainFilter,
    IReadOnlyCollection<string> CallerScopeTokens) : IQuery<Result<GlobalSearchResultDto>>;

internal sealed class GlobalSearchQueryHandler : IRequestHandler<GlobalSearchQuery, Result<GlobalSearchResultDto>>
{
    private const int _maxResults = 50;

    private readonly ISearchIndexDefinitionRepository _definitionRepository;
    private readonly IIndexedDocumentRepository _documentRepository;
    private readonly ISearchExecutionRepository _executionRepository;
    private readonly TimeProvider _timeProvider;

    public GlobalSearchQueryHandler(
        ISearchIndexDefinitionRepository definitionRepository,
        IIndexedDocumentRepository documentRepository,
        ISearchExecutionRepository executionRepository,
        TimeProvider timeProvider)
    {
        _definitionRepository = Guard.AgainstNull(definitionRepository, nameof(definitionRepository));
        _documentRepository = Guard.AgainstNull(documentRepository, nameof(documentRepository));
        _executionRepository = Guard.AgainstNull(executionRepository, nameof(executionRepository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<GlobalSearchResultDto>> Handle(GlobalSearchQuery request, CancellationToken cancellationToken)
    {
        var nowUtc = _timeProvider.GetUtcNow();

        var requestResult = SearchExecution.Request(request.TenantId, request.RequestedByUserId, request.QueryText, request.DomainFilter, nowUtc);
        if (requestResult.IsFailure)
        {
            return Result.Failure<GlobalSearchResultDto>(requestResult.Error);
        }

        var execution = requestResult.Value;

        SearchEntityType? domainFilterType = null;
        if (execution.DomainFilter is not null)
        {
            var domainFilterResult = SearchEntityType.Create(execution.DomainFilter);
            if (domainFilterResult.IsFailure)
            {
                execution.Fail("The requested search domain is not a recognized entity type.", _timeProvider.GetUtcNow());
                await _executionRepository.AddAsync(execution, cancellationToken).ConfigureAwait(false);
                return Result.Failure<GlobalSearchResultDto>(domainFilterResult.Error);
            }

            domainFilterType = domainFilterResult.Value;

            var definitionExists = await _definitionRepository.ExistsByEntityTypeAsync(domainFilterType, cancellationToken).ConfigureAwait(false);
            if (!definitionExists)
            {
                execution.Fail("No search index is registered for the requested domain.", _timeProvider.GetUtcNow());
                await _executionRepository.AddAsync(execution, cancellationToken).ConfigureAwait(false);
                return Result.Failure<GlobalSearchResultDto>(SearchErrors.SearchIndexDefinitionNotFound);
            }
        }

        var startingTimestamp = _timeProvider.GetTimestamp();

        var hits = await _documentRepository
            .SearchAsync(request.TenantId, execution.QueryText, domainFilterType, request.CallerScopeTokens, _maxResults, cancellationToken)
            .ConfigureAwait(false);

        var latencyMs = (long)_timeProvider.GetElapsedTime(startingTimestamp).TotalMilliseconds;

        execution.Complete(hits.Count, latencyMs, _timeProvider.GetUtcNow());
        await _executionRepository.AddAsync(execution, cancellationToken).ConfigureAwait(false);

        return Result.Success(SearchMapper.ToDto(execution, hits));
    }
}
