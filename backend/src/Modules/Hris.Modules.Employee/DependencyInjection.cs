using System.Reflection;
using FluentValidation;
using Hris.Infrastructure.Persistence;
using Hris.Modules.Employee.Domain;
using Hris.Modules.Employee.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Hris.Modules.Employee;

/// <summary>
/// Employee module's single registration entry point, per module-registration.md's
/// Module Entry Point section -- the identical shape
/// <c>Hris.Modules.Employment.ServiceCollectionExtensions</c> already establishes.
/// Fourth and last of Phase 2 (Core HR)'s own four Sprints (Organization, Position,
/// Employment, Employee); no compile-time dependency on any of the other three, or
/// on any sibling Phase 1 framework, per this platform's own standing "reference by
/// identifier, never by ProjectReference" rule.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEmployeeModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var thisAssembly = Assembly.GetExecutingAssembly();

        PersistenceAssemblyRegistry.Register(thisAssembly);

        services.AddMediatR(config => config.RegisterServicesFromAssembly(thisAssembly));
        services.AddValidatorsFromAssembly(thisAssembly);

        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IEmployeeHistoryRepository, EmployeeHistoryRepository>();

        return services;
    }
}
