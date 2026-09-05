using Hris.SharedKernel;

namespace Hris.Foundation.Caching.Domain;

/// <summary>
/// caching-framework.md's own Cache Key section: "Every cached object should have a
/// unique cache key... Keys should be deterministic and tenant-aware." Tenant id is a
/// required constructor parameter, never optional or defaulted, so a cache key
/// without tenant scope is unrepresentable -- the same "prefer structure over
/// discipline" resolution this platform already applies to CTR-ISO-001 elsewhere,
/// here for the identical risk this document's own AI Implementation Guidance names
/// by that CTR directly: "A cache key without tenant scope is a cross-tenant
/// disclosure."
///
/// <see cref="Region"/> is a validated string, not a closed enum, the identical
/// reasoning <c>PermissionKey.ResourceType</c> already establishes for itself: this
/// document's own Cache Region examples ("Employees, Payroll, Attendance,
/// Recruitment, Configuration, Security, Localization") are illustrative, and each
/// business module built from Phase 2 onward names its own region as it adopts
/// caching -- an enum here would require a change to this framework for every future
/// module, the same coupling a platform-wide framework must not impose.
/// </summary>
public sealed class CacheKey : ValueObject
{
    public Guid TenantId { get; }

    public string Region { get; }

    public string Key { get; }

    private CacheKey(Guid tenantId, string region, string key)
    {
        TenantId = tenantId;
        Region = region;
        Key = key;
    }

    public static Result<CacheKey> Create(Guid tenantId, string? region, string? key)
    {
        if (tenantId == Guid.Empty)
        {
            return Result.Failure<CacheKey>(CacheErrors.TenantIdRequired);
        }

        if (string.IsNullOrWhiteSpace(region))
        {
            return Result.Failure<CacheKey>(CacheErrors.RegionRequired);
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            return Result.Failure<CacheKey>(CacheErrors.KeyRequired);
        }

        return Result.Success(new CacheKey(tenantId, region.Trim(), key.Trim()));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return TenantId;
        yield return Region;
        yield return Key;
    }

    /// <summary>
    /// The deterministic wire form this key's own tenant-aware requirement produces --
    /// what <see cref="ICacheProvider"/>'s own Infrastructure implementation actually
    /// stores against.
    /// </summary>
    public override string ToString() => $"tenant:{TenantId}:region:{Region}:key:{Key}";
}
