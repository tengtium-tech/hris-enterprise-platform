using System.Reflection;
using FluentValidation;
using Hris.Foundation.StatutoryReferenceData.Domain;
using Hris.Foundation.StatutoryReferenceData.Infrastructure.Persistence;
using Hris.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Hris.Foundation.StatutoryReferenceData;

/// <summary>
/// Statutory Reference Data Framework's single registration entry point, per
/// module-registration.md's Module Entry Point section -- the identical shape every
/// Sprint 3/4 framework's own registration establishes. Eighth and last of Sprint 4's
/// own eight frameworks.
///
/// Of this framework's own three Upstream Dependencies (Localization, Configuration,
/// Audit), none is concretely wired through MediatR this Sprint, each for a stated
/// reason rather than by omission:
///
/// - Localization Framework: country scoping is satisfied by this framework's own local
///   <see cref="StatutoryCountryCode"/> Value Object (caller-supplied, ISO
///   3166-1-validated) rather than a live call into
///   <c>Hris.Foundation.Localization</c>'s own <c>CountryConfiguration</c> aggregate --
///   the same "duplicated Value Object, no cross-framework ProjectReference" choice that
///   class's own remarks state; a future integration point (validating a code against
///   Localization's own registered countries, not just ISO shape) is real future work,
///   not built here.
/// - Configuration Framework: this document's own stated dependency is "version
///   resolution" -- satisfied entirely by this framework's own
///   <see cref="IStatutoryTableVersionRepository.GetLatestEffectiveAsOfAsync"/>
///   (statutory-reference-data.md's own Selection Rule), which needs no tenant-configurable
///   value of Configuration Framework's own to resolve a table version, unlike, for
///   example, Numbering Framework's own reset-policy.
/// - Audit Framework: <c>IAuditRecorder.RecordAsync</c> requires a real tenant id to
///   populate the Event Framework envelope it also publishes -- this framework's own
///   aggregates deliberately carry none (statutory-reference-data.md's own Security
///   Considerations: "it is not tenant data"), so wiring it in would require inventing a
///   tenant id this framework's own domain has no legitimate place for, not merely a
///   missing concrete caller the way Search/Scheduling/Job Processing's own remarks
///   describe for themselves.
///
/// Every Domain Event this framework raises is dispatched through the same outbox
/// <see cref="Hris.Infrastructure"/>'s own <c>SaveChangesAsync</c> interceptor already
/// wires for every other framework -- no separate Event Framework integration point
/// needed here, the identical reasoning every other Sprint 4 framework's own remarks
/// state for itself.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStatutoryReferenceDataFramework(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var thisAssembly = Assembly.GetExecutingAssembly();

        PersistenceAssemblyRegistry.Register(thisAssembly);

        services.AddMediatR(config => config.RegisterServicesFromAssembly(thisAssembly));
        services.AddValidatorsFromAssembly(thisAssembly);

        services.AddScoped<IStatutoryProgramRepository, StatutoryProgramRepository>();
        services.AddScoped<IStatutoryTableVersionRepository, StatutoryTableVersionRepository>();

        return services;
    }
}
