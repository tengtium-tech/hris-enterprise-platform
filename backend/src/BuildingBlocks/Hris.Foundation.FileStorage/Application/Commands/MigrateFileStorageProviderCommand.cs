using Hris.Application.Abstractions;
using Hris.Foundation.FileStorage.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.FileStorage.Application.Commands;

/// <summary>
/// Relocates the current version's identical content to a different provider -- see
/// <see cref="StoredFile.MigrateCurrentVersionToProvider"/>.
/// <paramref name="MigratedContentChecksumValue"/> is the checksum of the copy already
/// written to the new provider/key, proving a faithful copy before this framework
/// updates any record of where the content lives.
/// </summary>
public sealed record MigrateFileStorageProviderCommand(
    Guid StoredFileId,
    StorageProviderType NewStorageProviderType,
    string NewStorageObjectKey,
    string MigratedContentChecksumValue) : ICommand<Result>;

internal sealed class MigrateFileStorageProviderCommandHandler : IRequestHandler<MigrateFileStorageProviderCommand, Result>
{
    private readonly IStoredFileRepository _repository;
    private readonly TimeProvider _timeProvider;

    public MigrateFileStorageProviderCommandHandler(IStoredFileRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(MigrateFileStorageProviderCommand request, CancellationToken cancellationToken)
    {
        var storedFile = await _repository.GetByIdAsync(new StoredFileId(request.StoredFileId), cancellationToken).ConfigureAwait(false);
        if (storedFile is null)
        {
            return Result.Failure(FileStorageErrors.StoredFileNotFound);
        }

        var keyResult = StorageObjectKey.Create(request.NewStorageObjectKey);
        if (keyResult.IsFailure)
        {
            return Result.Failure(keyResult.Error);
        }

        var checksumResult = Checksum.Create(ChecksumAlgorithm.Sha256, request.MigratedContentChecksumValue);
        if (checksumResult.IsFailure)
        {
            return Result.Failure(checksumResult.Error);
        }

        return storedFile.MigrateCurrentVersionToProvider(
            request.NewStorageProviderType, keyResult.Value, checksumResult.Value, _timeProvider.GetUtcNow());
    }
}
