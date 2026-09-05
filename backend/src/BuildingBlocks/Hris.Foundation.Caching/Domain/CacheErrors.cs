using Hris.SharedKernel;

namespace Hris.Foundation.Caching.Domain;

/// <summary>
/// This bounded context's own reusable error catalog, per error-pattern.md's "Error
/// Catalog" section.
/// </summary>
public static class CacheErrors
{
    public static readonly Error TenantIdRequired = new(
        "Caching.TenantIdRequired",
        "A cache key requires a tenant id -- caching-framework.md's own AI Implementation Guidance: a cache key without tenant scope is a cross-tenant disclosure (CTR-ISO-001).",
        ErrorCategory.Validation);

    public static readonly Error RegionRequired = new(
        "Caching.RegionRequired",
        "A cache key requires a region.",
        ErrorCategory.Validation);

    public static readonly Error KeyRequired = new(
        "Caching.KeyRequired",
        "A cache key requires a key value.",
        ErrorCategory.Validation);
}
