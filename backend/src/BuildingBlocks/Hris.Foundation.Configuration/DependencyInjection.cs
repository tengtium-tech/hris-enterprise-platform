using System.Reflection;
using FluentValidation;
using Hris.Foundation.Configuration.Domain;
using Hris.Foundation.Configuration.Infrastructure.Persistence;
using Hris.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Hris.Foundation.Configuration;

/// <summary>
/// The Configuration Framework's single registration entry point, per
/// module-registration.md's Module Entry Point section: "Each module exposes a single
/// extension method... This method becomes the only public registration API for the
/// module." Called from <c>Hris.Api</c>'s <c>Program.cs</c> during the
/// <c>AddFoundation()</c> step, before <c>AddInfrastructure()</c> --
/// <see cref="PersistenceAssemblyRegistry.Register"/> below must run before
/// <c>HrisDbContext</c> is ever constructed, per that registry's own remarks.
///
/// Assembly scanning here is limited to this framework's own assembly, per
/// module-registration.md's Assembly Scanning section ("Assembly scanning should be
/// limited to the module's own assemblies. Avoid scanning the entire application
/// domain.") -- <c>Hris.Api</c>'s <c>Program.cs</c> never scans a combined assembly
/// list itself; every framework and module calls its own <c>AddMediatR</c>/
/// <c>AddValidatorsFromAssembly</c>, which MediatR and FluentValidation both support
/// being called once per assembly without the registrations from an earlier call being
/// lost or duplicated.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddConfigurationFramework(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var thisAssembly = Assembly.GetExecutingAssembly();

        PersistenceAssemblyRegistry.Register(thisAssembly);

        services.AddMediatR(config => config.RegisterServicesFromAssembly(thisAssembly));
        services.AddValidatorsFromAssembly(thisAssembly);

        services.AddScoped<IConfigurationSettingRepository, ConfigurationSettingRepository>();
        services.AddScoped<ConfigurationHierarchyResolver>();

        return services;
    }
}
