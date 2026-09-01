using System.Reflection;
using FluentValidation;
using Hris.Foundation.Localization.Domain;
using Hris.Foundation.Localization.Infrastructure.Persistence;
using Hris.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Hris.Foundation.Localization;

/// <summary>
/// Localization Framework's single registration entry point, per
/// module-registration.md's Module Entry Point section -- the identical shape every
/// other Sprint 3 framework's own registration establishes.
///
/// Of this framework's own three Upstream Dependencies (Configuration, Audit,
/// Logging), only Configuration is concretely wired -- see
/// <see cref="Application.Queries.ResolveTranslationQuery"/>'s own remarks for its
/// fallback-locale-chain lookup. Audit Framework is deliberately not wired into any
/// write command here, even though localization-framework.md's own Security
/// Considerations names "Audit logging for localization changes": <c>IAuditRecorder.RecordAsync</c>
/// requires a real <c>tenantId</c> to populate the Event Framework envelope it also
/// publishes (`CTR-ISO-004`), and neither <see cref="Domain.CountryConfiguration"/>
/// nor <see cref="Domain.TranslationEntry"/> carries a tenant field in this Sprint's
/// own built Domain layer -- both are platform-wide catalogs, not tenant-scoped
/// ones. Inventing a placeholder tenant id to satisfy that signature would misrecord
/// every entry as belonging to a tenant it has nothing to do with, which is worse
/// than not auditing these writes at all; if a future Sprint gives either aggregate
/// real tenant scoping, wiring <c>IAuditRecorder</c> into these commands is the
/// natural next step. Logging Framework needs no explicit wiring here for the same
/// reason no other already-merged framework's own write commands call
/// <c>ILoggingService</c> directly: <c>UseSerilogRequestLogging()</c> already covers
/// every request generically at the host level (see <c>Program.cs</c>'s own
/// remarks).
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLocalizationFramework(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var thisAssembly = Assembly.GetExecutingAssembly();

        PersistenceAssemblyRegistry.Register(thisAssembly);

        services.AddMediatR(config => config.RegisterServicesFromAssembly(thisAssembly));
        services.AddValidatorsFromAssembly(thisAssembly);

        services.AddScoped<ICountryConfigurationRepository, CountryConfigurationRepository>();
        services.AddScoped<ITranslationEntryRepository, TranslationEntryRepository>();

        return services;
    }
}
