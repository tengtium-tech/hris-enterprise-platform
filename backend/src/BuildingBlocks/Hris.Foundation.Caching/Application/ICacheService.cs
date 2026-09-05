using Hris.Foundation.Caching.Domain;

namespace Hris.Foundation.Caching.Application;

/// <summary>
/// The Application-layer facade every other framework's and module's own code calls
/// directly to read or write a cache entry, per caching-framework.md's own framing:
/// "Business modules should use the Caching Framework for reusable, read-intensive
/// data rather than implementing independent caching mechanisms."
///
/// Deliberately not a MediatR <c>ICommand</c>/<c>IQuery</c>, the identical shape and
/// reasoning <c>ILoggingService</c> already establishes for itself: nothing in
/// caching-framework.md describes a user-driven business command lifecycle for a
/// cache read or write, and routing a cache write through <c>TransactionBehavior</c>
/// would commit a database transaction on every cache set -- a coupling this
/// document's own AI Implementation Guidance already rules out by treating the cache
/// as pure optimization ("every read path must remain correct with the cache empty
/// or unavailable").
///
/// Generic over <typeparamref name="T"/>: this framework itself never inspects the
/// cached value's own shape, only serializes and deserializes it -- callers decide
/// what is safe to cache, per this document's own "Never Cache Sensitive Secrets"
/// principle and its own AI Implementation Guidance ("Never cache sensitive personal
/// data or payroll values without an explicit, documented reason"), which this
/// generic surface cannot enforce structurally since it has no visibility into what
/// <typeparamref name="T"/> actually contains.
/// </summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(CacheKey key, CancellationToken cancellationToken);

    Task SetAsync<T>(CacheKey key, T value, CacheEntryOptions options, CancellationToken cancellationToken);

    Task RemoveAsync(CacheKey key, CancellationToken cancellationToken);

    /// <summary>
    /// caching-framework.md's own Cache Invalidation section: "Region Invalidation."
    /// Clears every entry cached under <paramref name="region"/> for
    /// <paramref name="tenantId"/> without disturbing any other region or tenant.
    /// </summary>
    Task ClearRegionAsync(Guid tenantId, string region, CancellationToken cancellationToken);

    /// <summary>
    /// caching-framework.md's own Cache Invalidation section: "Tenant-Level
    /// Invalidation." Clears every entry cached for <paramref name="tenantId"/>
    /// across every region.
    /// </summary>
    Task ClearTenantAsync(Guid tenantId, CancellationToken cancellationToken);
}
