namespace Hris.Foundation.FileStorage.Domain;

/// <summary>
/// Repository contract for the <see cref="StoredFile"/> Aggregate Root, per
/// repositories.md's "interface in the Domain layer... implementation in
/// Infrastructure" split.
/// </summary>
public interface IStoredFileRepository
{
    Task<StoredFile?> GetByIdAsync(StoredFileId id, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<StoredFile>> GetByContainerAsync(ContainerName containerName, CancellationToken cancellationToken);

    Task AddAsync(StoredFile storedFile, CancellationToken cancellationToken);
}
