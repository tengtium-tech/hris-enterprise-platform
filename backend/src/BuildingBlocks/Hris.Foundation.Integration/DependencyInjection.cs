using System.Reflection;
using FluentValidation;
using Hris.Foundation.Integration.Domain;
using Hris.Foundation.Integration.Infrastructure.Persistence;
using Hris.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Hris.Foundation.Integration;

/// <summary>
/// Integration Framework's single registration entry point, per
/// module-registration.md's Module Entry Point section -- the identical shape every
/// Sprint 3/4/5/8 framework's own registration establishes. Third and last of
/// Sprint 8's own three frameworks (Caching, Document Management, Integration
/// Framework), built as its own separate PR, the same "one framework, one PR, even
/// within one Sprint" discipline every earlier multi-framework Sprint already
/// establishes. With this framework registered, all of Sprint 8 is wired; Sprint 9
/// (Logging &amp; Monitoring Operational Layer) is next.
///
/// Of this framework's own six Upstream Dependencies (Event, Job Processing,
/// Configuration, Logging, Audit, Authorization), none is concretely wired through a
/// ProjectReference this Sprint, each for a stated reason rather than by omission:
///
/// - Job Processing Framework: this framework's own stated Sprint 4 dependency, yet
///   per this codebase's own established discipline (Notification Framework's own
///   remarks: "no Sprint 4/5 framework in this solution takes a ProjectReference on
///   another Sprint 3/4/5 framework's own project, and this one is no exception"),
///   <c>IntegrationRun.JobId</c> is a plain, optional, caller-supplied
///   <see cref="Guid"/> rather than a compile-time dependency -- see that property's
///   own remarks.
/// - Event Framework: every Domain Event this framework raises is dispatched through
///   the same outbox <see cref="Hris.Infrastructure"/>'s own <c>SaveChangesAsync</c>
///   interceptor already wires for every other framework -- no separate Event
///   Framework integration point needed here.
/// - Configuration Framework: this document's own Security Considerations state
///   "Secrets should be managed through the Configuration Framework or a dedicated
///   secrets management solution" -- a future credential-storage integration point,
///   not built here since this Sprint's own <see cref="Connector"/> stores only a
///   plain endpoint address, never a credential.
/// - Logging Framework: this document's own Observability requirement ("Provide
///   logs, metrics, traces...") is a cross-cutting concern every framework already
///   gets through ASP.NET Core's own logging pipeline, not something this framework's
///   own aggregates resolve internally.
/// - Audit Framework: identical reasoning to Event Framework above -- the outbox
///   already carries every raised event to whatever downstream consumer (including a
///   future Audit Framework projection) needs it.
/// - Authorization Framework: this document's own AI Implementation Guidance states
///   "Enforce authorization and entitlement identically on the API and the web
///   application" -- that check belongs to the caller (a business module's own
///   command handler, or a future API-layer policy), not to this framework's own
///   aggregate, the identical "authorization is the caller's job, not the resource's
///   own" split this codebase already applies everywhere else.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddIntegrationFramework(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var thisAssembly = Assembly.GetExecutingAssembly();

        PersistenceAssemblyRegistry.Register(thisAssembly);

        services.AddMediatR(config => config.RegisterServicesFromAssembly(thisAssembly));
        services.AddValidatorsFromAssembly(thisAssembly);

        services.AddScoped<IConnectorRepository, ConnectorRepository>();
        services.AddScoped<IIntegrationRunRepository, IntegrationRunRepository>();

        return services;
    }
}
