using System.Reflection;
using FluentValidation;
using Hris.Foundation.Search.Domain;
using Hris.Foundation.Search.Infrastructure.Persistence;
using Hris.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Hris.Foundation.Search;

/// <summary>
/// Search Framework's single registration entry point, per module-registration.md's
/// Module Entry Point section -- the identical shape every Sprint 3/4 framework's own
/// registration establishes.
///
/// Of this framework's own five Upstream Dependencies (Event, Configuration,
/// Authorization, Identity, Audit), none is concretely wired through MediatR this
/// Sprint, each for a stated reason rather than by omission:
///
/// - Authorization Framework: the structural hook for authorization trimming
///   (<c>IndexedDocument.SecurityScopeToken</c>, <c>GlobalSearchQuery.CallerScopeTokens</c>)
///   is built, but evaluating a caller's real permission grant against it needs a
///   concrete RBAC/ABAC service this Sprint's own build has no integration point for
///   yet -- the identical reasoning every other Sprint 4 framework's own remarks state.
///   Tenant isolation (<c>CTR-ISO-001</c>) is the one exception built concretely this
///   Sprint, not deferred with the rest -- see <see cref="IndexedDocument"/>'s own
///   remarks for why this framework's own AI Implementation Guidance treats it
///   differently from the general Authorization deferral.
/// - Audit Framework: <c>IAuditRecorder.RecordAsync</c> requires a real tenant id to
///   populate the Event Framework envelope it also publishes (<c>CTR-ISO-004</c>) --
///   this framework's own aggregates already carry a real tenant id, so the remaining
///   gap is <c>IAuditRecorder</c> itself having no concrete caller wired in yet, not a
///   missing tenant context.
/// - Configuration Framework: no tenant-configurable value this Sprint's own aggregate
///   behavior resolves (search-result page size, ranking weights, and similar are
///   fixed constants in this pass) -- a future per-tenant override is exactly the kind
///   of concrete integration point that would need it, not built here.
/// - Identity Framework: <c>RequestedByUserId</c>/<c>OwnerUserId</c> are plain
///   <see cref="Guid"/> values a caller supplies, the same "explicit, caller-supplied
///   value rather than resolved through a not-yet-wired framework" choice every
///   population-scale aggregate in this framework makes.
/// - Event Framework: every Domain Event this framework raises is dispatched through
///   the same outbox <see cref="Hris.Infrastructure"/>'s own <c>SaveChangesAsync</c>
///   interceptor already wires for every other framework -- no separate integration
///   point needed here.
///
/// Each is a real, stated Upstream Dependency this framework may call through MediatR
/// once a concrete integration point needs it -- not a gap.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSearchFramework(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var thisAssembly = Assembly.GetExecutingAssembly();

        PersistenceAssemblyRegistry.Register(thisAssembly);

        services.AddMediatR(config => config.RegisterServicesFromAssembly(thisAssembly));
        services.AddValidatorsFromAssembly(thisAssembly);

        services.AddScoped<ISearchIndexDefinitionRepository, SearchIndexDefinitionRepository>();
        services.AddScoped<IIndexedDocumentRepository, IndexedDocumentRepository>();
        services.AddScoped<ISearchExecutionRepository, SearchExecutionRepository>();
        services.AddScoped<ISavedSearchRepository, SavedSearchRepository>();

        return services;
    }
}
