using Hris.Application.Abstractions;
using Hris.Foundation.Search.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Search.Application.Commands;

/// <summary>
/// The two remaining <see cref="SearchIndexDefinition"/>-level operations -- update its
/// own field configuration, and record a completed full rebuild -- grouped into one
/// file the same way every other Sprint 3/4 framework's own bundled lifecycle commands
/// are (see <c>NumberSeriesLifecycleCommands</c>).
/// </summary>
public sealed record UpdateSearchIndexFieldsCommand(
    Guid SearchIndexDefinitionId,
    IReadOnlyList<SearchFieldDefinition> Fields,
    string? SecurityScopeKey) : ICommand<Result>;

internal sealed class UpdateSearchIndexFieldsCommandHandler : IRequestHandler<UpdateSearchIndexFieldsCommand, Result>
{
    private readonly ISearchIndexDefinitionRepository _repository;

    public UpdateSearchIndexFieldsCommandHandler(ISearchIndexDefinitionRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result> Handle(UpdateSearchIndexFieldsCommand request, CancellationToken cancellationToken)
    {
        var definition = await _repository
            .GetByIdAsync(new SearchIndexDefinitionId(request.SearchIndexDefinitionId), cancellationToken)
            .ConfigureAwait(false);

        return definition is null
            ? Result.Failure(SearchErrors.SearchIndexDefinitionNotFound)
            : definition.UpdateFields(request.Fields, request.SecurityScopeKey);
    }
}

/// <summary>
/// Called by whatever background process (outside this Sprint's own scope, see
/// <c>DependencyInjection.cs</c>) actually iterates a source module's records and calls
/// <see cref="IndexedDocument.Index"/>/<see cref="IndexedDocument.UpdateContent"/> for
/// each one, once that iteration completes -- this command only records the completion,
/// per <see cref="SearchIndexDefinition.CompleteRebuild"/>'s own remarks.
/// </summary>
public sealed record CompleteIndexRebuildCommand(Guid SearchIndexDefinitionId, int DocumentCount) : ICommand<Result>;

internal sealed class CompleteIndexRebuildCommandHandler : IRequestHandler<CompleteIndexRebuildCommand, Result>
{
    private readonly ISearchIndexDefinitionRepository _repository;
    private readonly TimeProvider _timeProvider;

    public CompleteIndexRebuildCommandHandler(ISearchIndexDefinitionRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(CompleteIndexRebuildCommand request, CancellationToken cancellationToken)
    {
        var definition = await _repository
            .GetByIdAsync(new SearchIndexDefinitionId(request.SearchIndexDefinitionId), cancellationToken)
            .ConfigureAwait(false);

        return definition is null
            ? Result.Failure(SearchErrors.SearchIndexDefinitionNotFound)
            : definition.CompleteRebuild(request.DocumentCount, _timeProvider.GetUtcNow());
    }
}
