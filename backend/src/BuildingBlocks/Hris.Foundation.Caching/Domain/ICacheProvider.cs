namespace Hris.Foundation.Caching.Domain;

/// <summary>
/// The Domain-owned contract a cache provider satisfies, per
/// docs/02-architecture/04-domain-driven-design/repositories.md's "interface in the
/// Domain layer... implementation in Infrastructure" split -- applied here to a cache
/// provider rather than an Aggregate Root's own repository, since
/// caching-framework.md's own Scope section explicitly excludes "Permanent Data
/// Storage": there is no Aggregate here to own a repository for.
///
/// Works in <see cref="string"/>, not a generic <c>T</c> -- <c>ICacheService</c>
/// (Application layer) owns serialization, so a future distributed provider (Redis,
/// per caching-framework.md's own Cache Provider section: "Providers should be
/// interchangeable") implements the identical string-in/string-out contract without
/// this framework's own callers, or this interface itself, changing at all.
///
/// <see cref="ClearRegionAsync"/> and <see cref="ClearTenantAsync"/> exist because
/// this document's own Cache Invalidation section names "Region Invalidation" and
/// "Tenant-Level Invalidation" as first-class capabilities -- a provider that can
/// only remove one key at a time cannot satisfy either without enumerating every key
/// it has ever stored, which <see cref="Microsoft.Extensions.Caching.Memory.IMemoryCache"/>
/// itself does not support. <see cref="Infrastructure.MemoryCacheProvider"/>'s own
/// remarks explain the token-based grouping mechanism this Sprint's own concrete
/// implementation uses instead.
/// </summary>
public interface ICacheProvider
{
    Task<string?> GetAsync(CacheKey key, CancellationToken cancellationToken);

    Task SetAsync(CacheKey key, string value, CacheEntryOptions options, CancellationToken cancellationToken);

    Task RemoveAsync(CacheKey key, CancellationToken cancellationToken);

    Task ClearRegionAsync(Guid tenantId, string region, CancellationToken cancellationToken);

    Task ClearTenantAsync(Guid tenantId, CancellationToken cancellationToken);
}
