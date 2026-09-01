using System.Reflection;
using FluentValidation;
using Hris.Foundation.Authorization.Domain;
using Hris.Foundation.Authorization.Infrastructure.Persistence;
using Hris.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Hris.Foundation.Authorization;

/// <summary>
/// Authorization Framework's single registration entry point, per module-registration.md's
/// Module Entry Point section -- the identical shape every other Sprint 3 framework's
/// own registration establishes: this framework defines its own
/// <c>IEntityTypeConfiguration&lt;T&gt;</c> classes and a real MediatR command/query
/// surface, so both <see cref="PersistenceAssemblyRegistry.Register"/> and
/// <c>AddMediatR</c> are called here.
///
/// Called from <c>Hris.Api</c>'s <c>Program.cs</c> during the <c>AddFoundation()</c>
/// step, after <c>AddEventFramework()</c> -- this framework's own stated Upstream
/// Dependencies list Identity (already built), Configuration (already built), Event
/// (already built), and Audit (not yet built, next in this Sprint's bootstrap order).
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAuthorizationFramework(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var thisAssembly = Assembly.GetExecutingAssembly();

        PersistenceAssemblyRegistry.Register(thisAssembly);

        services.AddMediatR(config => config.RegisterServicesFromAssembly(thisAssembly));
        services.AddValidatorsFromAssembly(thisAssembly);

        services.AddScoped<IRoleAssignmentRepository, RoleAssignmentRepository>();
        services.AddScoped<IRolePermissionGrantRepository, RolePermissionGrantRepository>();
        services.AddScoped<AuthorizationEvaluator>();

        return services;
    }
}
