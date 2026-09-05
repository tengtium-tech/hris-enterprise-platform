using System.Text.Json;
using Hris.Foundation.Caching.Domain;
using Hris.SharedKernel;

namespace Hris.Foundation.Caching.Application;

/// <summary>
/// Owns JSON serialization so <see cref="ICacheProvider"/> stays a plain
/// string-in/string-out contract regardless of which provider is registered -- see
/// that interface's own remarks for why.
/// </summary>
public sealed class CacheService : ICacheService
{
    private readonly ICacheProvider _provider;

    public CacheService(ICacheProvider provider)
    {
        _provider = Guard.AgainstNull(provider, nameof(provider));
    }

    public async Task<T?> GetAsync<T>(CacheKey key, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(key, nameof(key));

        var serialized = await _provider.GetAsync(key, cancellationToken).ConfigureAwait(false);

        return serialized is null ? default : JsonSerializer.Deserialize<T>(serialized);
    }

    public async Task SetAsync<T>(CacheKey key, T value, CacheEntryOptions options, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(key, nameof(key));
        Guard.AgainstNull(options, nameof(options));

        var serialized = JsonSerializer.Serialize(value);
        await _provider.SetAsync(key, serialized, options, cancellationToken).ConfigureAwait(false);
    }

    public async Task RemoveAsync(CacheKey key, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(key, nameof(key));

        await _provider.RemoveAsync(key, cancellationToken).ConfigureAwait(false);
    }

    public async Task ClearRegionAsync(Guid tenantId, string region, CancellationToken cancellationToken)
    {
        await _provider.ClearRegionAsync(tenantId, region, cancellationToken).ConfigureAwait(false);
    }

    public async Task ClearTenantAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        await _provider.ClearTenantAsync(tenantId, cancellationToken).ConfigureAwait(false);
    }
}
