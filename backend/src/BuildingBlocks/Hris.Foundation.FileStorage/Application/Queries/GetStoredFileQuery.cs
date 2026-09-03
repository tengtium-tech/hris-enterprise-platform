using Hris.Application.Abstractions;
using Hris.Foundation.FileStorage.Application.Dtos;
using Hris.Foundation.FileStorage.Application.Mapping;
using Hris.Foundation.FileStorage.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.FileStorage.Application.Queries;

/// <summary>
/// Reads one stored file back by its own identifier, including its current version
/// summary -- the shape a caller needing to download or display a file's own metadata
/// actually has in hand.
/// </summary>
public sealed record GetStoredFileQuery(Guid StoredFileId) : IQuery<Result<StoredFileDto>>;

internal sealed class GetStoredFileQueryHandler : IRequestHandler<GetStoredFileQuery, Result<StoredFileDto>>
{
    private readonly IStoredFileRepository _repository;

    public GetStoredFileQueryHandler(IStoredFileRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<StoredFileDto>> Handle(GetStoredFileQuery request, CancellationToken cancellationToken)
    {
        var storedFile = await _repository.GetByIdAsync(new StoredFileId(request.StoredFileId), cancellationToken).ConfigureAwait(false);

        return storedFile is null
            ? Result.Failure<StoredFileDto>(FileStorageErrors.StoredFileNotFound)
            : Result.Success(FileStorageMapper.ToDto(storedFile));
    }
}
