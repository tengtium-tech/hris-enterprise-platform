using Hris.Foundation.Caching.Application;
using Hris.Foundation.Caching.Domain;
using Hris.Foundation.Caching.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Hris.Foundation.Caching;

/// <summary>
/// Caching Framework's single registration entry point, per module-registration.md's
/// Module Entry Point section. Unlike a framework with its own persisted Aggregate,
/// this does not call <c>PersistenceAssemblyRegistry.Register</c> or <c>AddMediatR</c>
/// -- caching-framework.md's own Scope section excludes "Permanent Data Storage," and
/// this framework defines no MediatR requests (see <see cref="ICacheService"/>'s own
/// remarks).
///
/// This Sprint's own four stated Upstream Dependencies (Configuration, Event,
/// Logging, Tenant) are not concretely wired here: Configuration Framework would back
/// a future "configurable expiration per region" (caching-framework.md's own
/// Expiration Policies section: "Expiration should be configurable per cache
/// region"), deferred in favor of callers supplying <see cref="CacheEntryOptions"/>
/// explicitly for now; Event Framework would back a future event-driven invalidation
/// subscriber, deferred since no business module exists yet to publish the events
/// (EmployeeUpdated, PayrollConfigurationChanged) this document's own Cache
/// Invalidation section names as triggers; Logging and Tenant have no concrete call
/// site in this Sprint's own build either. Each is a real dependency with no
/// concrete integration point yet, not a gap -- the same "wired in incrementally as
/// siblings come online" resolution this codebase already applies elsewhere.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCachingFramework(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // AddMemoryCache() registers IMemoryCache as a singleton; MemoryCacheProvider
        // and CacheService are singletons for the same reason SerilogLogSink and
        // LoggingService are: neither holds a per-request resource, and IMemoryCache
        // itself is meant to be a long-lived singleton.
        services.AddMemoryCache();
        services.AddSingleton<ICacheProvider, MemoryCacheProvider>();
        services.AddSingleton<ICacheService, CacheService>();

        return services;
    }
}
