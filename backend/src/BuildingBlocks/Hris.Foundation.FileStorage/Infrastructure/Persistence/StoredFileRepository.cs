using Hris.Foundation.FileStorage.Domain;
using Hris.Infrastructure.Persistence;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hris.Foundation.FileStorage.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IStoredFileRepository"/>, per repositories.md's
/// "interface in the Domain layer... implementation in Infrastructure" split. No
/// <c>UpdateAsync</c>: an aggregate loaded through <see cref="GetByIdAsync"/> is already
/// tracked by this same <see cref="HrisDbContext"/>, so the caller's own
/// <c>TransactionBehavior</c> persists any mutation via change tracking alone.
/// </summary>
internal sealed class StoredFileRepository : IStoredFileRepository
{
    private readonly HrisDbContext _dbContext;

    public StoredFileRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public Task<StoredFile?> GetByIdAsync(StoredFileId id, CancellationToken cancellationToken) =>
        _dbContext.Set<StoredFile>().FirstOrDefaultAsync(storedFile => storedFile.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<StoredFile>> GetByContainerAsync(ContainerName containerName, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(containerName, nameof(containerName));

        return await _dbContext.Set<StoredFile>()
            .Where(storedFile => storedFile.ContainerName == containerName)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(StoredFile storedFile, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(storedFile, nameof(storedFile));
        await _dbContext.Set<StoredFile>().AddAsync(storedFile, cancellationToken).ConfigureAwait(false);
    }
}
