using Hris.Foundation.Localization.Domain;
using Hris.Infrastructure.Persistence;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hris.Foundation.Localization.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="ICountryConfigurationRepository"/>, per
/// repositories.md's "interface in the Domain layer... implementation in
/// Infrastructure" split. No <c>UpdateAsync</c>/<c>DeleteAsync</c>: an aggregate
/// loaded through <see cref="GetByCountryAsync"/> is already tracked by this same
/// <see cref="HrisDbContext"/>, so the caller's own <c>TransactionBehavior</c>
/// persists any mutation via change tracking alone when it calls
/// <c>SaveChangesAsync</c> -- the identical shape every other repository in this
/// Sprint already establishes.
/// </summary>
internal sealed class CountryConfigurationRepository : ICountryConfigurationRepository
{
    private readonly HrisDbContext _dbContext;

    public CountryConfigurationRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public Task<CountryConfiguration?> GetByCountryAsync(CountryCode country, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(country, nameof(country));

        // UNVERIFIED: this Value Object equality comparison translates to a correct
        // SQL WHERE clause the same way ConfigurationSettingRepository's own
        // GetByKeyAsync remarks flag for its own Key == key comparison -- flagged
        // here rather than silently assumed correct.
        return _dbContext.Set<CountryConfiguration>()
            .FirstOrDefaultAsync(configuration => configuration.Country == country, cancellationToken);
    }

    public async Task AddAsync(CountryConfiguration configuration, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(configuration, nameof(configuration));
        await _dbContext.Set<CountryConfiguration>().AddAsync(configuration, cancellationToken).ConfigureAwait(false);
    }
}
