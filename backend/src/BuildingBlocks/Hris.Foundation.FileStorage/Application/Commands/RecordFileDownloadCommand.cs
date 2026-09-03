using Hris.Application.Abstractions;
using Hris.Foundation.FileStorage.Domain;
using Hris.Foundation.Identity.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.FileStorage.Application.Commands;

/// <summary>
/// Audit-only record of a download -- see <see cref="StoredFile.RecordDownload"/>.
/// </summary>
public sealed record RecordFileDownloadCommand(Guid StoredFileId, Guid DownloadedByUserId) : ICommand<Result>;

internal sealed class RecordFileDownloadCommandHandler : IRequestHandler<RecordFileDownloadCommand, Result>
{
    private readonly IStoredFileRepository _repository;
    private readonly TimeProvider _timeProvider;

    public RecordFileDownloadCommandHandler(IStoredFileRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(RecordFileDownloadCommand request, CancellationToken cancellationToken)
    {
        var storedFile = await _repository.GetByIdAsync(new StoredFileId(request.StoredFileId), cancellationToken).ConfigureAwait(false);
        if (storedFile is null)
        {
            return Result.Failure(FileStorageErrors.StoredFileNotFound);
        }

        return storedFile.RecordDownload(new UserAccountId(request.DownloadedByUserId), _timeProvider.GetUtcNow());
    }
}
