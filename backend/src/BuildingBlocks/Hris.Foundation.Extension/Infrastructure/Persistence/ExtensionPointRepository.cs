using Hris.Foundation.Extension.Domain;
using Hris.Infrastructure.Persistence;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hris.Foundation.Extension.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IExtensionPointRepository"/>, per
/// repositories.md's "interface in the Domain layer... implementation in
/// Infrastructure" split. No <c>UpdateAsync</c>: an aggregate loaded through
/// <see cref="GetByIdAsync"/> is already tracked by this same <see cref="HrisDbContext"/>,
/// so the caller's own <c>TransactionBehavior</c> persists any mutation via change
/// tracking alone.
/// </summary>
internal sealed class ExtensionPointRepository : IExtensionPointRepository
{
    private readonly HrisDbContext _dbContext;

    public ExtensionPointRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public Task<ExtensionPoint?> GetByIdAsync(ExtensionPointId id, CancellationToken cancellationToken) =>
        _dbContext.Set<ExtensionPoint>().FirstOrDefaultAsync(extensionPoint => extensionPoint.Id == id, cancellationToken);

    // Key comparison against a HasConversion-mapped Value Object property: the same
    // shape HEP-38/HEP-51 already confirmed translates correctly against real
    // PostgreSQL for every other single-column Value Object comparison in this
    // codebase -- see coding-standards.md's own EF Core Persistence Pitfalls
    // subsection and Hris.Infrastructure.IntegrationTests.RepositoryQueryTranslationTests
    // for that precedent's own evidence. Not yet added as its own dedicated test in
    // that file -- tracked the same way, not assumed correct without ever checking.
    public Task<ExtensionPoint?> GetByKeyAsync(ExtensionPointKey key, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(key, nameof(key));

        return _dbContext.Set<ExtensionPoint>()
            .FirstOrDefaultAsync(extensionPoint => extensionPoint.Key == key, cancellationToken);
    }

    public Task<bool> ExistsByKeyAsync(ExtensionPointKey key, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(key, nameof(key));

        return _dbContext.Set<ExtensionPoint>()
            .AnyAsync(extensionPoint => extensionPoint.Key == key, cancellationToken);
    }

    public async Task<IReadOnlyCollection<ExtensionPoint>> GetAllAsync(CancellationToken cancellationToken) =>
        await _dbContext.Set<ExtensionPoint>().ToListAsync(cancellationToken).ConfigureAwait(false);

    public async Task AddAsync(ExtensionPoint extensionPoint, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(extensionPoint, nameof(extensionPoint));
        await _dbContext.Set<ExtensionPoint>().AddAsync(extensionPoint, cancellationToken).ConfigureAwait(false);
    }
}
