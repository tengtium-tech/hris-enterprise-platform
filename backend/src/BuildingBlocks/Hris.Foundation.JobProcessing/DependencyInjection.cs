using System.Reflection;
using FluentValidation;
using Hris.Foundation.JobProcessing.Domain;
using Hris.Foundation.JobProcessing.Infrastructure.Persistence;
using Hris.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Hris.Foundation.JobProcessing;

/// <summary>
/// Job Processing Framework's single registration entry point, per
/// module-registration.md's Module Entry Point section -- the identical shape every
/// Sprint 3/4 framework's own registration establishes.
///
/// Of this framework's own four Upstream Dependencies (Event, Configuration, Audit,
/// Logging), none is concretely wired through MediatR this Sprint, each for a stated
/// reason rather than by omission -- the identical four-way reasoning Scheduling
/// Framework's own remarks already state for the same four frameworks:
///
/// - Event Framework: every Domain Event this framework raises is dispatched through
///   the same outbox <see cref="Hris.Infrastructure"/>'s own <c>SaveChangesAsync</c>
///   interceptor already wires for every other framework.
/// - Audit Framework: this framework's own aggregates already carry a real tenant id
///   (<c>CTR-ISO-004</c>); the remaining gap is <c>IAuditRecorder</c> itself having no
///   concrete caller wired in yet.
/// - Configuration Framework: no tenant-configurable value this Sprint's own aggregate
///   behavior resolves -- <c>RegisterJobQueueCommand</c> already takes its own policy
///   explicitly from its own caller.
/// - Logging Framework: this framework raises no log entries of its own outside what
///   <c>Hris.Infrastructure</c>'s own cross-cutting behaviors already produce.
///
/// Genuinely out of this Sprint's own Scope, not merely deferred: this build records
/// job submission, queue policy, lifecycle state, and worker registration -- it does
/// not build the actual worker process that dequeues and executes a <see cref="Job"/>'s
/// own payload, does not enforce <see cref="JobQueue.MaxConcurrency"/> at dequeue time,
/// and does not schedule recurring jobs against Scheduling Framework's own
/// <c>Schedule</c> aggregate. job-processing.md's own Scope section excludes "Business
/// Workflow" and "Event Routing" outright; the remaining operational machinery
/// (auto-scaling, health-check heartbeats, actual concurrent dequeue-and-execute) is
/// real hosting/infrastructure work this Domain-and-Application-layer build does not
/// attempt to anticipate.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddJobProcessingFramework(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var thisAssembly = Assembly.GetExecutingAssembly();

        PersistenceAssemblyRegistry.Register(thisAssembly);

        services.AddMediatR(config => config.RegisterServicesFromAssembly(thisAssembly));
        services.AddValidatorsFromAssembly(thisAssembly);

        services.AddScoped<IJobQueueRepository, JobQueueRepository>();
        services.AddScoped<IJobRepository, JobRepository>();
        services.AddScoped<IWorkerRepository, WorkerRepository>();

        return services;
    }
}
