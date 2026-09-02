using Hris.Foundation.Extension.Domain;
using Hris.Infrastructure.Persistence;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hris.Foundation.Extension.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IHookRepository"/>, per repositories.md's
/// "interface in the Domain layer... implementation in Infrastructure" split.
/// </summary>
internal sealed class HookRepository : IHookRepository
{
    private readonly HrisDbContext _dbContext;

    public HookRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public Task<Hook?> GetByIdAsync(HookId id, CancellationToken cancellationToken) =>
        _dbContext.Set<Hook>().FirstOrDefaultAsync(hook => hook.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<Hook>> GetByExtensionPointIdAsync(ExtensionPointId extensionPointId, CancellationToken cancellationToken) =>
        await _dbContext.Set<Hook>()
            .Where(hook => hook.ExtensionPointId == extensionPointId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task AddAsync(Hook hook, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(hook, nameof(hook));
        await _dbContext.Set<Hook>().AddAsync(hook, cancellationToken).ConfigureAwait(false);
    }
}
