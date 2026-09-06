using System.Reflection;
using FluentValidation;
using Hris.Foundation.DocumentManagement.Domain;
using Hris.Foundation.DocumentManagement.Infrastructure.Persistence;
using Hris.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Hris.Foundation.DocumentManagement;

/// <summary>
/// Document Management Framework's single registration entry point, per
/// module-registration.md's Module Entry Point section -- the identical shape every
/// Sprint 3/4/5 framework's own registration establishes. Second of Sprint 8's own
/// three frameworks (Caching, Document Management, Integration Framework), built as
/// its own separate PR, the same "one framework, one PR, even within one Sprint"
/// discipline every earlier multi-framework Sprint already establishes.
///
/// Of this framework's own five Upstream Dependencies (Identity, Authorization, Audit,
/// Configuration, File Storage Framework), none is concretely wired through a
/// ProjectReference this Sprint, each for a stated reason rather than by omission:
///
/// - File Storage Framework: this document's own Scope explicitly excludes "Physical
///   File Storage" ("Physical storage is provided by the File Storage Framework"),
///   and per this codebase's own established discipline (Notification Framework's own
///   remarks: "no Sprint 4/5 framework in this solution takes a ProjectReference on
///   another Sprint 3/4/5 framework's own project, and this one is no exception"),
///   <c>DocumentVersion.StoredFileId</c> is a plain, caller-supplied <see cref="Guid"/>
///   rather than a compile-time dependency -- see that type's own remarks.
/// - Identity Framework: every user-identifying field on this framework's own
///   aggregates (<c>OwnerUserId</c>, <c>CreatedByUserId</c>, <c>AttachedByUserId</c>,
///   <c>DownloadedByUserId</c>) is a plain caller-supplied <see cref="Guid"/>, the same
///   "explicit parameter rather than an ambient identity service" choice every other
///   Sprint 4/5 framework's own remarks already state.
/// - Authorization Framework: this framework's own AI Implementation Guidance states
///   "Apply authorization to document access, not only to the record the document is
///   attached to" -- that check belongs to the caller (a business module's own command
///   handler, or a future API-layer policy), not to this framework's own aggregate,
///   the identical "authorization is the caller's job, not the resource's own" split
///   this codebase already applies everywhere else.
/// - Audit Framework: every Domain Event this framework raises is dispatched through
///   the same outbox <see cref="Hris.Infrastructure"/>'s own <c>SaveChangesAsync</c>
///   interceptor already wires for every other framework -- no separate Audit
///   Framework integration point needed here.
/// - Configuration Framework: no tenant-configurable value this Sprint's own aggregate
///   behavior resolves internally -- a future per-tenant retention policy is exactly
///   the kind of concrete integration point that would need it, not built here since
///   this Sprint's own <see cref="Document"/> takes its own dates from the caller
///   rather than resolving a policy internally.
///
/// With this framework registered, two of Sprint 8's own three frameworks are wired;
/// Integration Framework is next.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDocumentManagementFramework(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var thisAssembly = Assembly.GetExecutingAssembly();

        PersistenceAssemblyRegistry.Register(thisAssembly);

        services.AddMediatR(config => config.RegisterServicesFromAssembly(thisAssembly));
        services.AddValidatorsFromAssembly(thisAssembly);

        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IDocumentAttachmentRepository, DocumentAttachmentRepository>();

        return services;
    }
}
