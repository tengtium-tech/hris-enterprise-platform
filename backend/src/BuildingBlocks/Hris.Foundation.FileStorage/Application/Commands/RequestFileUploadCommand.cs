using Hris.Application.Abstractions;
using Hris.Foundation.FileStorage.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.FileStorage.Application.Commands;

/// <summary>
/// Begins the first upload for a new logical file, per file-storage.md's own File
/// Lifecycle ("Upload Requested"). Carries raw primitives, not Domain Value Objects,
/// across the MediatR boundary -- <see cref="RequestFileUploadCommandHandler"/> is the
/// one place a malformed container name or file name becomes a
/// <see cref="FileStorageErrors"/> failure.
/// </summary>
public sealed record RequestFileUploadCommand(string ContainerName, string OriginalFileName) : ICommand<Result<Guid>>;

internal sealed class RequestFileUploadCommandHandler : IRequestHandler<RequestFileUploadCommand, Result<Guid>>
{
    private readonly IStoredFileRepository _repository;

    public RequestFileUploadCommandHandler(IStoredFileRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<Guid>> Handle(RequestFileUploadCommand request, CancellationToken cancellationToken)
    {
        var result = StoredFile.RequestUpload(request.ContainerName, request.OriginalFileName);
        if (result.IsFailure)
        {
            return Result.Failure<Guid>(result.Error);
        }

        await _repository.AddAsync(result.Value, cancellationToken).ConfigureAwait(false);

        return Result.Success(result.Value.Id.Value);
    }
}

/// <summary>
/// Begins a second or later version's upload against a file already
/// <see cref="FileLifecycleStatus.Available"/> -- see <see cref="StoredFile.RequestNewVersionUpload"/>.
/// </summary>
public sealed record RequestNewFileVersionUploadCommand(Guid StoredFileId) : ICommand<Result>;

internal sealed class RequestNewFileVersionUploadCommandHandler : IRequestHandler<RequestNewFileVersionUploadCommand, Result>
{
    private readonly IStoredFileRepository _repository;

    public RequestNewFileVersionUploadCommandHandler(IStoredFileRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result> Handle(RequestNewFileVersionUploadCommand request, CancellationToken cancellationToken)
    {
        var storedFile = await _repository.GetByIdAsync(new StoredFileId(request.StoredFileId), cancellationToken).ConfigureAwait(false);
        if (storedFile is null)
        {
            return Result.Failure(FileStorageErrors.StoredFileNotFound);
        }

        return storedFile.RequestNewVersionUpload();
    }
}
