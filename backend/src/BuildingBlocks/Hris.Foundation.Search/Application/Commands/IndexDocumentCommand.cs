using Hris.Application.Abstractions;
using Hris.Foundation.Search.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Search.Application.Commands;

/// <summary>
/// The common "put this record in the index" path -- looks up whether
/// <see cref="IndexedDocument.SourceEntityType"/>/<see cref="IndexedDocument.SourceEntityId"/>/
/// <see cref="IndexedDocument.TenantId"/> already has an indexed document and either
/// creates one (<see cref="IndexedDocument.Index"/>) or updates it
/// (<see cref="IndexedDocument.UpdateContent"/>), folded into one command because a
/// source module publishing "this record changed" has no reason to know or care whether
/// this is the first time -- the identical collapsing choice
/// <c>RequestAndReserveNumberCommand</c>'s own remarks make for its own two-step path.
/// </summary>
public sealed record IndexDocumentCommand(
    string SourceEntityType,
    string SourceEntityId,
    Guid TenantId,
    string SearchableContent,
    string? SecurityScopeToken) : ICommand<Result<Guid>>;

internal sealed class IndexDocumentCommandHandler : IRequestHandler<IndexDocumentCommand, Result<Guid>>
{
    private readonly ISearchIndexDefinitionRepository _definitionRepository;
    private readonly IIndexedDocumentRepository _documentRepository;
    private readonly TimeProvider _timeProvider;

    public IndexDocumentCommandHandler(
        ISearchIndexDefinitionRepository definitionRepository,
        IIndexedDocumentRepository documentRepository,
        TimeProvider timeProvider)
    {
        _definitionRepository = Guard.AgainstNull(definitionRepository, nameof(definitionRepository));
        _documentRepository = Guard.AgainstNull(documentRepository, nameof(documentRepository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(IndexDocumentCommand request, CancellationToken cancellationToken)
    {
        var entityTypeResult = SearchEntityType.Create(request.SourceEntityType);
        if (entityTypeResult.IsFailure)
        {
            return Result.Failure<Guid>(entityTypeResult.Error);
        }

        var entityType = entityTypeResult.Value;

        var definition = await _definitionRepository.GetByEntityTypeAsync(entityType, cancellationToken).ConfigureAwait(false);
        if (definition is null)
        {
            return Result.Failure<Guid>(SearchErrors.SearchIndexDefinitionNotFound);
        }

        var nowUtc = _timeProvider.GetUtcNow();

        var existing = await _documentRepository
            .FindBySourceAsync(entityType, request.SourceEntityId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            var updateResult = existing.UpdateContent(request.SearchableContent, request.SecurityScopeToken, nowUtc);
            return updateResult.IsFailure ? Result.Failure<Guid>(updateResult.Error) : Result.Success(existing.Id.Value);
        }

        var indexResult = IndexedDocument.Index(
            definition.Id, entityType, request.SourceEntityId, request.TenantId, request.SearchableContent, request.SecurityScopeToken, nowUtc);
        if (indexResult.IsFailure)
        {
            return Result.Failure<Guid>(indexResult.Error);
        }

        await _documentRepository.AddAsync(indexResult.Value, cancellationToken).ConfigureAwait(false);

        return Result.Success(indexResult.Value.Id.Value);
    }
}
