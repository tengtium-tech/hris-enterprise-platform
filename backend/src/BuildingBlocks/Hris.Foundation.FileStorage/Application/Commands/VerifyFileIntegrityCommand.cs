using Hris.Application.Abstractions;
using Hris.Foundation.FileStorage.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.FileStorage.Application.Commands;

/// <summary>
/// The upload-time integrity check -- see <see cref="StoredFile.VerifyIntegrity"/>.
/// </summary>
public sealed record VerifyFileIntegrityCommand(Guid StoredFileId, string ActualChecksumValue) : ICommand<Result>;

internal sealed class VerifyFileIntegrityCommandHandler : IRequestHandler<VerifyFileIntegrityCommand, Result>
{
    private readonly IStoredFileRepository _repository;
    private readonly TimeProvider _timeProvider;

    public VerifyFileIntegrityCommandHandler(IStoredFileRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(VerifyFileIntegrityCommand request, CancellationToken cancellationToken)
    {
        var storedFile = await _repository.GetByIdAsync(new StoredFileId(request.StoredFileId), cancellationToken).ConfigureAwait(false);
        if (storedFile is null)
        {
            return Result.Failure(FileStorageErrors.StoredFileNotFound);
        }

        var checksumResult = Checksum.Create(ChecksumAlgorithm.Sha256, request.ActualChecksumValue);
        if (checksumResult.IsFailure)
        {
            return Result.Failure(checksumResult.Error);
        }

        return storedFile.VerifyIntegrity(checksumResult.Value, _timeProvider.GetUtcNow());
    }
}

/// <summary>
/// The separate, later, periodic re-check of an already-available file's own current
/// version -- see <see cref="StoredFile.ReverifyIntegrity"/>.
/// </summary>
public sealed record ReverifyFileIntegrityCommand(Guid StoredFileId, string ActualChecksumValue) : ICommand<Result>;

internal sealed class ReverifyFileIntegrityCommandHandler : IRequestHandler<ReverifyFileIntegrityCommand, Result>
{
    private readonly IStoredFileRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ReverifyFileIntegrityCommandHandler(IStoredFileRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ReverifyFileIntegrityCommand request, CancellationToken cancellationToken)
    {
        var storedFile = await _repository.GetByIdAsync(new StoredFileId(request.StoredFileId), cancellationToken).ConfigureAwait(false);
        if (storedFile is null)
        {
            return Result.Failure(FileStorageErrors.StoredFileNotFound);
        }

        var checksumResult = Checksum.Create(ChecksumAlgorithm.Sha256, request.ActualChecksumValue);
        if (checksumResult.IsFailure)
        {
            return Result.Failure(checksumResult.Error);
        }

        return storedFile.ReverifyIntegrity(checksumResult.Value, _timeProvider.GetUtcNow());
    }
}
