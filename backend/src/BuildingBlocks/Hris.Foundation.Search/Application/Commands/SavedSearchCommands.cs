using Hris.Application.Abstractions;
using Hris.Foundation.Search.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Search.Application.Commands;

/// <summary>
/// The three <see cref="SavedSearch"/> operations -- save, rename, delete -- grouped
/// into one file the same way every other Sprint 3/4 framework's own bundled
/// lifecycle commands are (see <c>NumberSeriesLifecycleCommands</c>).
/// </summary>
public sealed record SaveSearchCommand(
    Guid TenantId, Guid OwnerUserId, string Name, string QueryText, string? DomainFilter) : ICommand<Result<Guid>>;

internal sealed class SaveSearchCommandHandler : IRequestHandler<SaveSearchCommand, Result<Guid>>
{
    private readonly ISavedSearchRepository _repository;
    private readonly TimeProvider _timeProvider;

    public SaveSearchCommandHandler(ISavedSearchRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(SaveSearchCommand request, CancellationToken cancellationToken)
    {
        var saveResult = SavedSearch.Save(
            request.TenantId, request.OwnerUserId, request.Name, request.QueryText, request.DomainFilter, _timeProvider.GetUtcNow());
        if (saveResult.IsFailure)
        {
            return Result.Failure<Guid>(saveResult.Error);
        }

        await _repository.AddAsync(saveResult.Value, cancellationToken).ConfigureAwait(false);

        return Result.Success(saveResult.Value.Id.Value);
    }
}

public sealed record RenameSavedSearchCommand(Guid SavedSearchId, string NewName) : ICommand<Result>;

internal sealed class RenameSavedSearchCommandHandler : IRequestHandler<RenameSavedSearchCommand, Result>
{
    private readonly ISavedSearchRepository _repository;

    public RenameSavedSearchCommandHandler(ISavedSearchRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result> Handle(RenameSavedSearchCommand request, CancellationToken cancellationToken)
    {
        var savedSearch = await _repository
            .GetByIdAsync(new SavedSearchId(request.SavedSearchId), cancellationToken)
            .ConfigureAwait(false);

        return savedSearch is null
            ? Result.Failure(SearchErrors.SavedSearchNotFound)
            : savedSearch.Rename(request.NewName);
    }
}

public sealed record DeleteSavedSearchCommand(Guid SavedSearchId) : ICommand<Result>;

internal sealed class DeleteSavedSearchCommandHandler : IRequestHandler<DeleteSavedSearchCommand, Result>
{
    private readonly ISavedSearchRepository _repository;

    public DeleteSavedSearchCommandHandler(ISavedSearchRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result> Handle(DeleteSavedSearchCommand request, CancellationToken cancellationToken)
    {
        var savedSearch = await _repository
            .GetByIdAsync(new SavedSearchId(request.SavedSearchId), cancellationToken)
            .ConfigureAwait(false);

        if (savedSearch is null)
        {
            return Result.Failure(SearchErrors.SavedSearchNotFound);
        }

        await _repository.DeleteAsync(savedSearch, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
