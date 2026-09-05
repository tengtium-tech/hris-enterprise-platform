namespace Hris.Foundation.Caching.Domain;

/// <summary>
/// caching-framework.md's own Expiration Policies section: "Absolute Expiration,
/// Sliding Expiration, Never Expire (with caution)." The remaining three named
/// strategies -- Event-Based Invalidation, Manual Invalidation, Scheduled
/// Invalidation -- are not TTL policies at all; they are actions a caller performs
/// (<c>ICacheService.RemoveAsync</c>, <c>ClearRegionAsync</c>, <c>ClearTenantAsync</c>),
/// triggered by an event handler, an operator, or a scheduled job respectively, not a
/// property stored alongside the entry itself. <see cref="NeverExpire"/> is this
/// document's own "(with caution)" qualifier made explicit: choosing it is a
/// deliberate act, not the type's own default silently reached by omission.
/// </summary>
public sealed record CacheEntryOptions(TimeSpan? AbsoluteExpiration = null, TimeSpan? SlidingExpiration = null)
{
    public static CacheEntryOptions NeverExpire { get; } = new();
}
