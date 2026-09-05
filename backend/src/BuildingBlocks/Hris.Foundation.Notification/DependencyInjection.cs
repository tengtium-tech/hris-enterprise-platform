using System.Reflection;
using FluentValidation;
using Hris.Foundation.Notification.Domain;
using Hris.Foundation.Notification.Infrastructure.Persistence;
using Hris.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Hris.Foundation.Notification;

/// <summary>
/// Notification Framework's single registration entry point, per
/// module-registration.md's Module Entry Point section -- the identical shape every
/// Sprint 3/4 framework's own registration establishes. Second and last of Sprint 5's
/// own two frameworks (Workflow Engine and Notification Framework), built as its own
/// separate PR the same "one framework, one PR, even within one Sprint" discipline
/// every earlier multi-framework Sprint already establishes.
///
/// Of this framework's own five Upstream Dependencies (Workflow Engine, Rules Engine,
/// Identity, Configuration, Event Framework), none is concretely wired through MediatR
/// this Sprint, each for a stated reason rather than by omission:
///
/// - Workflow Engine: the genuine mutual-dependency cycle IMPLEMENTATION-PLAN.md and
///   both frameworks' own Jira Stories name is a same-Sprint pairing, not a same-PR or
///   same-ProjectReference one -- no Sprint 4/5 framework in this solution takes a
///   `ProjectReference` on another Sprint 3/4/5 framework's own project, and this one
///   is no exception. A workflow step's own <c>NotificationTemplateKey</c> is a plain,
///   opaque string reference for exactly this reason, the same "generic reference, not
///   a strongly-typed cross-framework FK" choice <c>WorkflowStepDefinition.ActionName</c>'s
///   own remarks already establish for itself.
/// - Rules Engine: this document's own Scope explicitly excludes "Business Rule
///   Evaluation" -- there is no condition/business-policy evaluation for this
///   framework to route through Rules Engine.
/// - Identity Framework: every user-identifying field on this framework's own
///   aggregates (`RecipientUserId`, `ActorUserId`, `UserId`) is a plain caller-supplied
///   <see cref="Guid"/>, the identical "explicit parameter rather than an ambient
///   identity service" choice every other Sprint 4/5 framework's own remarks already
///   state.
/// - Configuration Framework: no tenant-configurable value this Sprint's own aggregate
///   behavior resolves internally -- a future per-tenant default retry policy or quiet-
///   hours enforcement rule is exactly the kind of concrete integration point that
///   would need it, not built here since every transition this Sprint exposes takes
///   its own timing and policy from the caller rather than resolving one internally.
///
/// Every Domain Event this framework raises is dispatched through the same outbox
/// <see cref="Hris.Infrastructure"/>'s own <c>SaveChangesAsync</c> interceptor already
/// wires for every other framework -- no separate Event Framework integration point
/// needed here, the identical reasoning every other Sprint 4/5 framework's own remarks
/// state for itself. With this framework registered, both of Sprint 5's own two
/// frameworks are wired; Sprint 6 (Entitlement &amp; Process Pack Framework) is next.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNotificationFramework(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var thisAssembly = Assembly.GetExecutingAssembly();

        PersistenceAssemblyRegistry.Register(thisAssembly);

        services.AddMediatR(config => config.RegisterServicesFromAssembly(thisAssembly));
        services.AddValidatorsFromAssembly(thisAssembly);

        services.AddScoped<INotificationTemplateRepository, NotificationTemplateRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INotificationPreferenceRepository, NotificationPreferenceRepository>();

        return services;
    }
}
