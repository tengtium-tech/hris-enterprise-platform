using Hris.Foundation.Localization.Domain;
using Hris.Infrastructure.Persistence;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hris.Foundation.Localization.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="ITranslationEntryRepository"/>, per
/// repositories.md's "interface in the Domain layer... implementation in
/// Infrastructure" split. No <c>UpdateAsync</c>/<c>DeleteAsync</c> -- the identical
/// shape <see cref="CountryConfigurationRepository"/>'s own remarks explain.
/// </summary>
internal sealed class TranslationEntryRepository : ITranslationEntryRepository
{
    private readonly HrisDbContext _dbContext;

    public TranslationEntryRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public Task<TranslationEntry?> GetByKeyAsync(string key, CancellationToken cancellationToken)
    {
        Guard.AgainstNullOrWhiteSpace(key, nameof(key));

        return _dbContext.Set<TranslationEntry>()
            .FirstOrDefaultAsync(entry => entry.Key == key, cancellationToken);
    }

    public async Task AddAsync(TranslationEntry entry, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(entry, nameof(entry));
        await _dbContext.Set<TranslationEntry>().AddAsync(entry, cancellationToken).ConfigureAwait(false);
    }
}
