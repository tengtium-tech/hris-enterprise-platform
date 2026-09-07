using System.Reflection;
using FluentValidation;
using Hris.Infrastructure.Persistence;
using Hris.Modules.Organization.Domain;
using Hris.Modules.Organization.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Hris.Modules.Organization;

/// <summary>
/// Organization module's single registration entry point, per
/// module-registration.md's Module Entry Point section, the identical shape every
/// Sprint 3-9 Foundation framework's own registration establishes -- extended here
/// to the platform's first business module. First of Phase 2 (Core HR)'s own four
/// Sprints (Organization, Position, Employment, Employee); this module has no
/// compile-time dependency on any of the other three, or on any sibling Phase 1
/// framework, per this platform's own standing "reference by identifier, never by
/// ProjectReference" rule -- <see cref="Domain.Organization.LegalEntityId"/> and
/// <see cref="WorkLocation.OrganizationId"/> are both plain <see cref="Guid"/>s for
/// exactly that reason.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOrganizationModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var thisAssembly = Assembly.GetExecutingAssembly();

        PersistenceAssemblyRegistry.Register(thisAssembly);

        services.AddMediatR(config => config.RegisterServicesFromAssembly(thisAssembly));
        services.AddValidatorsFromAssembly(thisAssembly);

        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<IWorkLocationRepository, WorkLocationRepository>();
        services.AddScoped<ILegalEntityRepository, LegalEntityRepository>();

        return services;
    }
}
