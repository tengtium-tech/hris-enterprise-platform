using System.Reflection;
using FluentValidation;
using Hris.Foundation.FileStorage.Domain;
using Hris.Foundation.FileStorage.Infrastructure.Persistence;
using Hris.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Hris.Foundation.FileStorage;

/// <summary>
/// File Storage Framework's single registration entry point, per
/// module-registration.md's Module Entry Point section -- the identical shape every
/// Sprint 3/4 framework's own registration establishes.
///
/// Of this framework's own four Upstream Dependencies (Identity, Authorization, Audit,
/// Configuration), only Identity is concretely used in this Sprint's own build --
/// <see cref="Domain.FileVersion.UploadedByUserId"/> and the download-recording
/// command's own actor are both a real <c>UserAccountId</c>, not a placeholder Guid.
/// The remaining three are each deferred for a stated reason, matching the identical
/// precedent every other Sprint 3/4 framework's own registration already documents for
/// at least one of its own nominally-unused dependencies:
///
/// - Authorization Framework: no aggregate in this Sprint's own build (or any prior
///   Sprint 4 framework's) carries a tenant field yet, and
///   <c>OrganizationalScopeLevel</c> has nothing concrete to check a mutation against
///   without inventing one -- the identical reasoning Extension Framework's own remarks
///   state for its own deferred wiring. A future integration point exists the moment a
///   business module calling into this framework supplies real tenant/organizational
///   context, which this Sprint's own commands do not yet require callers to pass.
/// - Audit Framework: <c>IAuditRecorder.RecordAsync</c> requires a real tenant id to
///   populate the Event Framework envelope it also publishes (`CTR-ISO-004`), the
///   identical missing-tenant-context reason above.
/// - Configuration Framework: no tenant-configurable value this Sprint's own aggregate
///   behavior resolves -- a future retention-policy integration (file-storage.md's own
///   Storage Lifecycle Management section: "Archive after X Days... Automatic
///   Deletion... Legal Hold") is exactly the kind of concrete integration point that
///   would need it, deliberately not built here since this framework's own Scope
///   excludes policy *ownership* (a tenant's retention policy is configuration data
///   belonging to whichever module or framework administers it, not a business rule
///   this framework's own aggregate invents on its own).
///
/// Each is a real, stated Upstream Dependency this framework may call through MediatR
/// once a concrete integration point needs it -- not a gap.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFileStorageFramework(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var thisAssembly = Assembly.GetExecutingAssembly();

        PersistenceAssemblyRegistry.Register(thisAssembly);

        services.AddMediatR(config => config.RegisterServicesFromAssembly(thisAssembly));
        services.AddValidatorsFromAssembly(thisAssembly);

        services.AddScoped<IStoredFileRepository, StoredFileRepository>();

        return services;
    }
}
