using System.Reflection;
using FluentValidation;
using Hris.Infrastructure.Persistence;
using Hris.Modules.Position.Domain;
using Hris.Modules.Position.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Hris.Modules.Position;

/// <summary>
/// Position module's single registration entry point, per module-registration.md's
/// Module Entry Point section -- the identical shape
/// <c>Hris.Modules.Organization.ServiceCollectionExtensions</c> already establishes.
/// Second of Phase 2 (Core HR)'s own four Sprints (Organization, Position,
/// Employment, Employee); no compile-time dependency on any of the other three, or
/// on any sibling Phase 1 framework, per this platform's own standing "reference by
/// identifier, never by ProjectReference" rule -- every organizational and
/// workforce-classification reference on <see cref="Domain.Position"/> is a plain
/// <see cref="Guid"/> for exactly that reason.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPositionModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var thisAssembly = Assembly.GetExecutingAssembly();

        PersistenceAssemblyRegistry.Register(thisAssembly);

        services.AddMediatR(config => config.RegisterServicesFromAssembly(thisAssembly));
        services.AddValidatorsFromAssembly(thisAssembly);

        services.AddScoped<IPositionRepository, PositionRepository>();
        services.AddScoped<IJobFamilyRepository, JobFamilyRepository>();
        services.AddScoped<IJobClassificationRepository, JobClassificationRepository>();
        services.AddScoped<IJobGradeRepository, JobGradeRepository>();

        return services;
    }
}
