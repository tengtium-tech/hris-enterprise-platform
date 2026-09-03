using System.Reflection;
using FluentValidation;
using Hris.Foundation.Scheduling.Domain;
using Hris.Foundation.Scheduling.Infrastructure.Persistence;
using Hris.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Hris.Foundation.Scheduling;

/// <summary>
/// Scheduling Framework's single registration entry point, per module-registration.md's
/// Module Entry Point section -- the identical shape every Sprint 3/4 framework's own
/// registration establishes.
///
/// Of this framework's own four Upstream Dependencies (Configuration, Event, Audit,
/// Logging), none is concretely wired through MediatR this Sprint, each for a stated
/// reason rather than by omission:
///
/// - Event Framework: every Domain Event this framework raises is dispatched through
///   the same outbox <see cref="Hris.Infrastructure"/>'s own <c>SaveChangesAsync</c>
///   interceptor already wires for every other framework -- no separate integration
///   point needed here.
/// - Audit Framework: <c>IAuditRecorder.RecordAsync</c> requires a real tenant id to
///   populate the Event Framework envelope it also publishes -- this framework's own
///   aggregates already carry one, so the remaining gap is <c>IAuditRecorder</c> itself
///   having no concrete caller wired in yet, not a missing tenant context, the identical
///   reasoning Search Framework's own remarks state for itself.
/// - Configuration Framework: no tenant-configurable value this Sprint's own aggregate
///   behavior resolves -- a future per-tenant default (for example a standard holiday
///   behavior) is exactly the kind of concrete integration point that would need it,
///   not built here since <c>CreateScheduleCommand</c> already takes holiday behavior
///   explicitly from its own caller.
/// - Logging Framework: this framework raises no log entries of its own outside what
///   <c>Hris.Infrastructure</c>'s own cross-cutting behaviors already produce for every
///   MediatR request; a concrete integration point would be a scheduler-specific
///   diagnostic this Sprint's own build does not yet need.
///
/// Job Processing Framework, this framework's own first-listed Downstream Consumer, is
/// not yet built (a later Sprint 4 framework, no forced order) -- <c>ScheduleExecution</c>'s
/// own <c>JobIdentifier</c> is a generic, nullable string reference for exactly this
/// reason, the same "generic reference, not a strongly-typed FK to an unbuilt
/// framework" choice this framework's own remarks make throughout.
///
/// Each is a real, stated Upstream Dependency this framework may call through MediatR
/// once a concrete integration point needs it -- not a gap.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSchedulingFramework(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var thisAssembly = Assembly.GetExecutingAssembly();

        PersistenceAssemblyRegistry.Register(thisAssembly);

        services.AddMediatR(config => config.RegisterServicesFromAssembly(thisAssembly));
        services.AddValidatorsFromAssembly(thisAssembly);

        services.AddScoped<IScheduleRepository, ScheduleRepository>();
        services.AddScoped<IScheduleExecutionRepository, ScheduleExecutionRepository>();

        return services;
    }
}
