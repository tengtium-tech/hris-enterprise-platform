using System.Reflection;
using FluentValidation;
using Hris.Foundation.Numbering.Domain;
using Hris.Foundation.Numbering.Infrastructure.Persistence;
using Hris.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Hris.Foundation.Numbering;

/// <summary>
/// Numbering Framework's single registration entry point, per module-registration.md's
/// Module Entry Point section -- the identical shape every Sprint 3/4 framework's own
/// registration establishes.
///
/// Of this framework's own three Upstream Dependencies (Configuration, Audit,
/// Authorization), none is concretely wired through MediatR this Sprint, each for a
/// stated reason rather than by omission:
///
/// - Authorization Framework: no aggregate this Sprint's own build carries a tenant
///   field, and <c>OrganizationalScopeLevel</c> has nothing concrete to check a
///   mutation against without inventing one -- the identical reasoning every other
///   Sprint 4 framework's own remarks state for its own deferred wiring.
/// - Audit Framework: <c>IAuditRecorder.RecordAsync</c> requires a real tenant id to
///   populate the Event Framework envelope it also publishes (`CTR-ISO-004`), the
///   identical missing-tenant-context reason above.
/// - Configuration Framework: no tenant-configurable value this Sprint's own aggregate
///   behavior resolves -- a future per-tenant default format/reset-policy integration
///   is exactly the kind of concrete integration point that would need it, not built
///   here since <c>RegisterNumberSeriesCommand</c> already takes format and
///   reset-policy explicitly from its own caller.
///
/// Each is a real, stated Upstream Dependency this framework may call through MediatR
/// once a concrete integration point needs it -- not a gap.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNumberingFramework(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var thisAssembly = Assembly.GetExecutingAssembly();

        PersistenceAssemblyRegistry.Register(thisAssembly);

        services.AddMediatR(config => config.RegisterServicesFromAssembly(thisAssembly));
        services.AddValidatorsFromAssembly(thisAssembly);

        services.AddScoped<INumberSeriesRepository, NumberSeriesRepository>();
        services.AddScoped<IIssuedNumberRepository, IssuedNumberRepository>();

        return services;
    }
}
