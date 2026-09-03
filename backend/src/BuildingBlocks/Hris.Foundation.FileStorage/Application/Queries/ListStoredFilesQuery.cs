using Hris.Application.Abstractions;
using Hris.Foundation.FileStorage.Application.Dtos;
using Hris.Foundation.FileStorage.Application.Mapping;
using Hris.Foundation.FileStorage.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.FileStorage.Application.Queries;

/// <summary>
/// Every stored file within one logical container -- file-storage.md's own Bucket/
/// Container concept ("Files should be organized into logical containers").
/// </summary>
public sealed record ListStoredFilesQuery(string ContainerName) : IQuery<Result<IReadOnlyCollection<StoredFileDto>>>;

internal sealed class ListStoredFilesQueryHandler
    : IRequestHandler<ListStoredFilesQuery, Result<IReadOnlyCollection<StoredFileDto>>>
{
    private readonly IStoredFileRepository _repository;

    public ListStoredFilesQueryHandler(IStoredFileRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<IReadOnlyCollection<StoredFileDto>>> Handle(
        ListStoredFilesQuery request,
        CancellationToken cancellationToken)
    {
        var containerNameResult = ContainerName.Create(request.ContainerName);
        if (containerNameResult.IsFailure)
        {
            return Result.Failure<IReadOnlyCollection<StoredFileDto>>(containerNameResult.Error);
        }

        var storedFiles = await _repository.GetByContainerAsync(containerNameResult.Value, cancellationToken).ConfigureAwait(false);
        IReadOnlyCollection<StoredFileDto> dtos = storedFiles.Select(FileStorageMapper.ToDto).ToList();
        return Result.Success(dtos);
    }
}
