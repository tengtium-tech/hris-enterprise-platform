using Hris.Application.Abstractions;
using Hris.Foundation.FileStorage.Domain;
using Hris.Foundation.Identity.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.FileStorage.Application.Commands;

/// <summary>
/// Confirms real content was written to a provider -- see
/// <see cref="StoredFile.MarkUploaded"/>. Every content fact (key, checksum, size, MIME
/// type, provider) is carried here as the authoritative, just-confirmed truth, never
/// re-derived from whatever a caller declared at <c>RequestFileUploadCommand</c> time.
/// </summary>
public sealed record ConfirmFileUploadCommand(
    Guid StoredFileId,
    string StorageObjectKey,
    string ChecksumValue,
    long FileSizeBytes,
    string MimeType,
    StorageProviderType StorageProviderType,
    Guid UploadedByUserId) : ICommand<Result>;

internal sealed class ConfirmFileUploadCommandHandler : IRequestHandler<ConfirmFileUploadCommand, Result>
{
    private readonly IStoredFileRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ConfirmFileUploadCommandHandler(IStoredFileRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ConfirmFileUploadCommand request, CancellationToken cancellationToken)
    {
        var storedFile = await _repository.GetByIdAsync(new StoredFileId(request.StoredFileId), cancellationToken).ConfigureAwait(false);
        if (storedFile is null)
        {
            return Result.Failure(FileStorageErrors.StoredFileNotFound);
        }

        var keyResult = StorageObjectKey.Create(request.StorageObjectKey);
        if (keyResult.IsFailure)
        {
            return Result.Failure(keyResult.Error);
        }

        var checksumResult = Checksum.Create(ChecksumAlgorithm.Sha256, request.ChecksumValue);
        if (checksumResult.IsFailure)
        {
            return Result.Failure(checksumResult.Error);
        }

        var mimeTypeResult = MimeType.Create(request.MimeType);
        if (mimeTypeResult.IsFailure)
        {
            return Result.Failure(mimeTypeResult.Error);
        }

        return storedFile.MarkUploaded(
            keyResult.Value,
            checksumResult.Value,
            request.FileSizeBytes,
            mimeTypeResult.Value,
            request.StorageProviderType,
            new UserAccountId(request.UploadedByUserId),
            _timeProvider.GetUtcNow());
    }
}
