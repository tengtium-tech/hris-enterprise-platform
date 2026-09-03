using Hris.Application.Abstractions;
using Hris.Foundation.FileStorage.Application.Dtos;
using Hris.Foundation.FileStorage.Application.Mapping;
using Hris.Foundation.FileStorage.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.FileStorage.Application.Queries;

/// <summary>
/// The full, immutable version history of one stored file, per file-storage.md's File
/// Versioning section ("Previous versions should remain recoverable when enabled").
/// </summary>
public sealed record ListFileVersionsQuery(Guid StoredFileId) : IQuery<Result<IReadOnlyCollection<FileVersionDto>>>;

internal sealed class ListFileVersionsQueryHandler
    : IRequestHandler<ListFileVersionsQuery, Result<IReadOnlyCollection<FileVersionDto>>>
{
    private readonly IStoredFileRepository _repository;

    public ListFileVersionsQueryHandler(IStoredFileRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<IReadOnlyCollection<FileVersionDto>>> Handle(
        ListFileVersionsQuery request,
        CancellationToken cancellationToken)
    {
        var storedFile = await _repository.GetByIdAsync(new StoredFileId(request.StoredFileId), cancellationToken).ConfigureAwait(false);
        if (storedFile is null)
        {
            return Result.Failure<IReadOnlyCollection<FileVersionDto>>(FileStorageErrors.StoredFileNotFound);
        }

        IReadOnlyCollection<FileVersionDto> dtos = storedFile.Versions.Select(FileStorageMapper.ToDto).ToList();
        return Result.Success(dtos);
    }
}
