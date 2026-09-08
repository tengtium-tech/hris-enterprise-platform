using System.Reflection;
using FluentValidation;
using Hris.Infrastructure.Persistence;
using Hris.Modules.Employment.Domain;
using Hris.Modules.Employment.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Hris.Modules.Employment;

/// <summary>
/// Employment module's single registration entry point, per module-registration.md's
/// Module Entry Point section -- the identical shape
/// <c>Hris.Modules.Position.ServiceCollectionExtensions</c> already establishes.
/// Third of Phase 2 (Core HR)'s own four Sprints (Organization, Position, Employment,
/// Employee); no compile-time dependency on any of the other three, or on any
/// sibling Phase 1 framework, per this platform's own standing "reference by
/// identifier, never by ProjectReference" rule.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEmploymentModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var thisAssembly = Assembly.GetExecutingAssembly();

        PersistenceAssemblyRegistry.Register(thisAssembly);

        services.AddMediatR(config => config.RegisterServicesFromAssembly(thisAssembly));
        services.AddValidatorsFromAssembly(thisAssembly);

        services.AddScoped<IEmploymentRepository, EmploymentRepository>();
        services.AddScoped<IEmploymentContractRepository, EmploymentContractRepository>();
        services.AddScoped<IEmploymentAssignmentRepository, EmploymentAssignmentRepository>();

        return services;
    }
}
