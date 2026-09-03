using Hris.Application.Abstractions;
using Hris.Foundation.FileStorage.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.FileStorage.Application.Commands;

/// <summary>
/// Confirms durable storage and availability in one step -- see
/// <see cref="StoredFile.MarkStored"/>.
/// </summary>
public sealed record ConfirmFileStoredCommand(Guid StoredFileId) : ICommand<Result>;

internal sealed class ConfirmFileStoredCommandHandler : IRequestHandler<ConfirmFileStoredCommand, Result>
{
    private readonly IStoredFileRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ConfirmFileStoredCommandHandler(IStoredFileRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ConfirmFileStoredCommand request, CancellationToken cancellationToken)
    {
        var storedFile = await _repository.GetByIdAsync(new StoredFileId(request.StoredFileId), cancellationToken).ConfigureAwait(false);
        if (storedFile is null)
        {
            return Result.Failure(FileStorageErrors.StoredFileNotFound);
        }

        return storedFile.MarkStored(_timeProvider.GetUtcNow());
    }
}
