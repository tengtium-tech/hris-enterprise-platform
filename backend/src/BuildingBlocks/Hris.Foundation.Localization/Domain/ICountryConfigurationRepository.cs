namespace Hris.Foundation.Localization.Domain;

/// <summary>
/// Persistence abstraction for <see cref="CountryConfiguration"/>, per
/// repositories.md's "interface in the Domain layer... implementation in
/// Infrastructure" split. No Infrastructure implementation exists yet
/// (backend/README.md).
/// </summary>
public interface ICountryConfigurationRepository
{
    Task<CountryConfiguration?> GetByCountryAsync(CountryCode country, CancellationToken cancellationToken);

    Task AddAsync(CountryConfiguration configuration, CancellationToken cancellationToken);
}
