using System.Reflection;
using FluentValidation;
using Hris.Foundation.WorkflowEngine.Domain;
using Hris.Foundation.WorkflowEngine.Infrastructure.Persistence;
using Hris.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Hris.Foundation.WorkflowEngine;

/// <summary>
/// Workflow Engine Framework's single registration entry point, per
/// module-registration.md's Module Entry Point section -- the identical shape every
/// Sprint 3/4 framework's own registration establishes. First of Sprint 5's own two
/// frameworks (Workflow Engine and Notification Framework), built as a genuine mutual
/// dependency cycle per IMPLEMENTATION-PLAN.md's own Sprint 5 row, but as two separate
/// PRs, the same "one framework, one PR, even within a single Sprint" discipline
/// Sprint 3's own nine-framework kernel and Sprint 4's own eight frameworks already
/// establish -- the cycle affects which Sprint each belongs to, not how many PRs
/// deliver it.
///
/// Of this framework's own five Upstream Dependencies (Identity, Authorization, Rules
/// Engine, Configuration, Notification), none is concretely wired through MediatR this
/// Sprint, each for a stated reason rather than by omission:
///
/// - Notification Framework: workflow-engine.md's own Actions section states plainly
///   "Notification actions are delegated to the Notification Framework rather than
///   implemented within the engine" -- <see cref="WorkflowStepDefinition.NotificationTemplateKey"/>
///   is a generic, opaque string reference for exactly this reason, since Notification
///   Framework does not exist in code yet (this Sprint's own second framework, no
///   forced order within it). The genuine mutual-dependency cycle IMPLEMENTATION-PLAN.md
///   and this framework's own Jira Story both name is a same-Sprint pairing, not a
///   same-PR one; wiring the concrete call happens once both sides exist.
/// - Identity/Authorization Frameworks: every user-identifying field on this
///   framework's own aggregates (<c>InitiatedByUserId</c>, <c>AssignedToUserId</c>,
///   <c>DelegatedToUserId</c>) is a plain caller-supplied <see cref="Guid"/>, the
///   identical "explicit parameter rather than an ambient identity/authorization
///   service" choice every other Sprint 4/5 framework's own remarks already state;
///   resolving "who may act on this approval" and "who is the requester's own reporting
///   manager" (<see cref="WorkflowParticipantType.DynamicManager"/>) is Authorization
///   Framework's and a future Employment-module integration's own concrete concern, not
///   built here.
/// - Rules Engine: workflow-engine.md's own Conditions section states plainly "Complex
///   business rule evaluation belongs to the Rules Engine rather than to workflow
///   conditions" -- the actual condition/branch evaluator that would call it is this
///   Sprint's own deliberately excluded runtime (see this framework's own csproj header
///   for the full scope boundary), so there is no concrete call site for it yet either.
/// - Configuration Framework: no tenant-configurable value this Sprint's own aggregate
///   behavior resolves -- a future per-tenant SLA/escalation interval default is
///   exactly the kind of concrete integration point that would need it, not built here
///   since every escalation/expiry transition this Sprint exposes takes its own timing
///   from the caller rather than resolving one internally.
///
/// Every Domain Event this framework raises is dispatched through the same outbox
/// <see cref="Hris.Infrastructure"/>'s own <c>SaveChangesAsync</c> interceptor already
/// wires for every other framework -- no separate Event Framework integration point
/// needed here, the identical reasoning every other Sprint 4/5 framework's own remarks
/// state for itself.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWorkflowEngineFramework(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var thisAssembly = Assembly.GetExecutingAssembly();

        PersistenceAssemblyRegistry.Register(thisAssembly);

        services.AddMediatR(config => config.RegisterServicesFromAssembly(thisAssembly));
        services.AddValidatorsFromAssembly(thisAssembly);

        services.AddScoped<IWorkflowDefinitionRepository, WorkflowDefinitionRepository>();
        services.AddScoped<IWorkflowInstanceRepository, WorkflowInstanceRepository>();
        services.AddScoped<IWorkflowTaskRepository, WorkflowTaskRepository>();

        return services;
    }
}
