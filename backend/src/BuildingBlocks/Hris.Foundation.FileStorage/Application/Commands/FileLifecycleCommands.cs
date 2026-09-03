using Hris.Application.Abstractions;
using Hris.Foundation.FileStorage.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.FileStorage.Application.Commands;

/// <summary>
/// The three remaining post-availability lifecycle transitions -- Archive, Restore,
/// Delete -- grouped into one file the same way every other Sprint 3/4 framework's own
/// bundled lifecycle commands are. Each handler is the same shape: look the aggregate up
/// by id, fail with <see cref="FileStorageErrors.StoredFileNotFound"/> if it does not
/// exist, otherwise call the one Domain method and return its own <see cref="Result"/>.
/// None needs an explicit save: the aggregate was already loaded through this same
/// <c>DbContext</c>, so the caller's own <c>TransactionBehavior</c> persists the
/// mutation via change tracking alone.
/// </summary>
public sealed record ArchiveFileCommand(Guid StoredFileId) : ICommand<Result>;

internal sealed class ArchiveFileCommandHandler : IRequestHandler<ArchiveFileCommand, Result>
{
    private readonly IStoredFileRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ArchiveFileCommandHandler(IStoredFileRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ArchiveFileCommand request, CancellationToken cancellationToken)
    {
        var storedFile = await _repository.GetByIdAsync(new StoredFileId(request.StoredFileId), cancellationToken).ConfigureAwait(false);
        if (storedFile is null)
        {
            return Result.Failure(FileStorageErrors.StoredFileNotFound);
        }

        return storedFile.Archive(_timeProvider.GetUtcNow());
    }
}

public sealed record RestoreFileCommand(Guid StoredFileId) : ICommand<Result>;

internal sealed class RestoreFileCommandHandler : IRequestHandler<RestoreFileCommand, Result>
{
    private readonly IStoredFileRepository _repository;
    private readonly TimeProvider _timeProvider;

    public RestoreFileCommandHandler(IStoredFileRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(RestoreFileCommand request, CancellationToken cancellationToken)
    {
        var storedFile = await _repository.GetByIdAsync(new StoredFileId(request.StoredFileId), cancellationToken).ConfigureAwait(false);
        if (storedFile is null)
        {
            return Result.Failure(FileStorageErrors.StoredFileNotFound);
        }

        return storedFile.Restore(_timeProvider.GetUtcNow());
    }
}

public sealed record DeleteFileCommand(Guid StoredFileId) : ICommand<Result>;

internal sealed class DeleteFileCommandHandler : IRequestHandler<DeleteFileCommand, Result>
{
    private readonly IStoredFileRepository _repository;
    private readonly TimeProvider _timeProvider;

    public DeleteFileCommandHandler(IStoredFileRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(DeleteFileCommand request, CancellationToken cancellationToken)
    {
        var storedFile = await _repository.GetByIdAsync(new StoredFileId(request.StoredFileId), cancellationToken).ConfigureAwait(false);
        if (storedFile is null)
        {
            return Result.Failure(FileStorageErrors.StoredFileNotFound);
        }

        return storedFile.Delete(_timeProvider.GetUtcNow());
    }
}
