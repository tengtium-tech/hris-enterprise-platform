using System.Collections.Concurrent;
using Hris.Foundation.Caching.Domain;
using Hris.SharedKernel;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace Hris.Foundation.Caching.Infrastructure;

/// <summary>
/// This Sprint's own one concrete <see cref="ICacheProvider"/>, wrapping
/// <see cref="IMemoryCache"/> -- an in-process, single-instance cache, per
/// caching-framework.md's own Local Cache section ("Application instance memory").
/// A future distributed provider (Redis) implements the identical interface without
/// <see cref="Application.CacheService"/> or any caller changing.
///
/// <see cref="IMemoryCache"/> has no native "remove by prefix" or "enumerate keys"
/// operation, so region- and tenant-level invalidation cannot be implemented by
/// scanning stored keys. Instead, every entry is registered against two
/// <see cref="CancellationChangeToken"/>s at write time -- one shared by every entry
/// in its own tenant+region group, one shared by every entry in its own tenant across
/// all regions. <see cref="ClearRegionAsync"/>/<see cref="ClearTenantAsync"/> cancel
/// and replace the corresponding <see cref="CancellationTokenSource"/>, which
/// <see cref="IMemoryCache"/> itself evicts every registered entry for automatically.
/// This is the standard documented technique for group eviction against
/// <see cref="IMemoryCache"/>, not a workaround specific to this framework.
///
/// A narrow, accepted race exists between a concurrent <see cref="SetAsync"/> and a
/// clear for the same group: <see cref="ConcurrentDictionary{TKey,TValue}.GetOrAdd"/>
/// could return the about-to-be-cancelled token an instant before
/// <see cref="ConcurrentDictionary{TKey,TValue}.TryRemove"/> replaces it, in which
/// case that one entry is evicted immediately rather than surviving the clear -- an
/// over-eviction, never an under-eviction, and therefore never a staleness or
/// cross-tenant risk, only a possible extra cache miss.
/// </summary>
public sealed class MemoryCacheProvider : ICacheProvider
{
    private readonly IMemoryCache _cache;
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _regionGroups = new();
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _tenantGroups = new();
    private long _hits;
    private long _misses;

    public MemoryCacheProvider(IMemoryCache cache)
    {
        _cache = Guard.AgainstNull(cache, nameof(cache));
    }

    public Task<string?> GetAsync(CacheKey key, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(key, nameof(key));

        if (_cache.TryGetValue(key.ToString(), out string? value))
        {
            Interlocked.Increment(ref _hits);
            return Task.FromResult(value);
        }

        Interlocked.Increment(ref _misses);
        return Task.FromResult<string?>(null);
    }

    public Task SetAsync(CacheKey key, string value, CacheEntryOptions options, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(key, nameof(key));
        Guard.AgainstNull(value, nameof(value));
        Guard.AgainstNull(options, nameof(options));

        var entryOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = options.AbsoluteExpiration,
            SlidingExpiration = options.SlidingExpiration,
        };

        entryOptions.ExpirationTokens.Add(new CancellationChangeToken(GetRegionGroup(key.TenantId, key.Region).Token));
        entryOptions.ExpirationTokens.Add(new CancellationChangeToken(GetTenantGroup(key.TenantId).Token));

        _cache.Set(key.ToString(), value, entryOptions);

        return Task.CompletedTask;
    }

    public Task RemoveAsync(CacheKey key, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(key, nameof(key));

        _cache.Remove(key.ToString());

        return Task.CompletedTask;
    }

    public Task ClearRegionAsync(Guid tenantId, string region, CancellationToken cancellationToken)
    {
        CancelAndReplace(_regionGroups, RegionGroupKey(tenantId, region));

        return Task.CompletedTask;
    }

    public Task ClearTenantAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        CancelAndReplace(_tenantGroups, TenantGroupKey(tenantId));

        return Task.CompletedTask;
    }

    /// <summary>
    /// This Sprint's own hit/miss counters, surfaced directly on the concrete
    /// provider rather than added to <see cref="ICacheProvider"/> itself --
    /// caching-framework.md's own Monitoring section names these as an NFR-level
    /// "should," not a Scope-level contract member, and a future distributed
    /// provider is more likely to expose equivalent metrics through its own
    /// vendor-specific monitoring than through this interface.
    /// </summary>
    public CacheStatistics GetStatistics() => new(Interlocked.Read(ref _hits), Interlocked.Read(ref _misses));

    private CancellationTokenSource GetRegionGroup(Guid tenantId, string region) =>
        _regionGroups.GetOrAdd(RegionGroupKey(tenantId, region), static _ => new CancellationTokenSource());

    private CancellationTokenSource GetTenantGroup(Guid tenantId) =>
        _tenantGroups.GetOrAdd(TenantGroupKey(tenantId), static _ => new CancellationTokenSource());

    private static void CancelAndReplace(ConcurrentDictionary<string, CancellationTokenSource> groups, string groupKey)
    {
        if (groups.TryRemove(groupKey, out var cancelled))
        {
            cancelled.Cancel();
            cancelled.Dispose();
        }
    }

    private static string RegionGroupKey(Guid tenantId, string region) => $"tenant:{tenantId}:region:{region}";

    private static string TenantGroupKey(Guid tenantId) => $"tenant:{tenantId}";
}

/// <summary>caching-framework.md's own Monitoring section: "Cache Hits... Cache Misses... Hit Ratio."</summary>
public readonly record struct CacheStatistics(long Hits, long Misses)
{
    public double HitRatio => Hits + Misses == 0 ? 0 : (double)Hits / (Hits + Misses);
}
