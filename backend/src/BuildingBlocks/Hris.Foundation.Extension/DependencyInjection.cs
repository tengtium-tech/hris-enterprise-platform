using System.Reflection;
using FluentValidation;
using Hris.Foundation.Extension.Domain;
using Hris.Foundation.Extension.Infrastructure.Persistence;
using Hris.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Hris.Foundation.Extension;

/// <summary>
/// Extension Framework's single registration entry point, per module-registration.md's
/// Module Entry Point section -- the identical shape every Sprint 3/4 framework's own
/// registration establishes.
///
/// Of this framework's own six Upstream Dependencies (Event, Validation, Authorization,
/// Configuration, Logging, Audit), none is concretely wired through MediatR in this
/// Sprint's own build, each for a stated reason rather than by omission:
///
/// - Authorization Framework: <c>RegisterExtensionPointCommand</c>'s own remarks state
///   why its write commands are deliberately ungated, matching Localization Framework's
///   own established precedent rather than Rules Engine's -- an Extension Point is
///   platform-wide contract-registry data, and <c>OrganizationalScopeLevel</c> has no
///   Global level to check a platform-wide mutation against.
/// - Audit Framework: <c>IAuditRecorder.RecordAsync</c> requires a real tenant id to
///   populate the Event Framework envelope it also publishes (`CTR-ISO-004`), and
///   neither <see cref="Domain.ExtensionPoint"/> nor <see cref="Domain.Hook"/> carries a
///   tenant field -- both are platform-wide registries, not tenant-scoped data, the
///   identical reasoning Localization Framework's own remarks state for its own
///   deferred Audit wiring.
/// - Event Framework: publishing this framework's own domain events through Event
///   Framework's own outbox would hit the identical missing-tenant-context problem
///   Audit wiring does, plus the same reasoning Rules Engine's own remarks state for
///   why it does not publish its own lifecycle events through Event Framework either.
/// - Configuration Framework: no configurable value either aggregate's own behavior
///   needs to resolve.
/// - Validation Framework: FluentValidation (the library) is used directly, the same
///   as every other framework's own Application-layer validators; <c>IValidationService</c>
///   itself (the Application-layer facade a *caller* of a command might use for
///   policy-based severity) has no concrete integration point here yet.
/// - Logging Framework: <c>UseSerilogRequestLogging()</c> already covers every request
///   generically at the host level, the same reasoning stated for every other
///   already-merged framework's own DI file.
///
/// Each is a real, stated Upstream Dependency this framework may call through MediatR
/// once a concrete integration point needs it -- not a gap, the same "wire it in when
/// a real caller exists" precedent every other Sprint 3/4 framework's own
/// DependencyInjection.cs already documents for at least one of its own
/// nominally-unused dependencies.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddExtensionFramework(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var thisAssembly = Assembly.GetExecutingAssembly();

        PersistenceAssemblyRegistry.Register(thisAssembly);

        services.AddMediatR(config => config.RegisterServicesFromAssembly(thisAssembly));
        services.AddValidatorsFromAssembly(thisAssembly);

        services.AddScoped<IExtensionPointRepository, ExtensionPointRepository>();
        services.AddScoped<IHookRepository, HookRepository>();

        return services;
    }
}
