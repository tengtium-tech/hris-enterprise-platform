using System.Reflection;
using FluentValidation;
using Hris.Infrastructure.Persistence;
using Hris.Modules.Administration.Domain;
using Hris.Modules.Administration.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Hris.Modules.Administration;

/// <summary>
/// Administration module's single registration entry point, per
/// module-registration.md's Module Entry Point section, the identical shape
/// <c>Hris.Modules.Employee.ServiceCollectionExtensions</c> already establishes.
/// First module of Phase 3 (Workforce Management), and first module overall since
/// Phase 2 (Core HR) closed; no compile-time dependency on any Phase 2 module or
/// any Phase 1 framework, per this platform's own standing "reference by
/// identifier, never by ProjectReference" rule.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAdministrationModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var thisAssembly = Assembly.GetExecutingAssembly();

        PersistenceAssemblyRegistry.Register(thisAssembly);

        services.AddMediatR(config => config.RegisterServicesFromAssembly(thisAssembly));
        services.AddValidatorsFromAssembly(thisAssembly);

        services.AddScoped<IUserAccountRepository, UserAccountRepository>();
        services.AddScoped<ITenantRoleRepository, TenantRoleRepository>();
        services.AddScoped<IAdministrativeDelegationRepository, AdministrativeDelegationRepository>();

        return services;
    }
}
