using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Hris.Foundation.Entitlement;

/// <summary>
/// Entitlement Framework's single registration entry point, per module-registration.md's
/// Module Entry Point section. Unlike every framework with its own persisted
/// Aggregate, this one does not call <c>PersistenceAssemblyRegistry.Register</c> --
/// entitlement-framework.md's own Scope section states why: no
/// <c>IEntityTypeConfiguration&lt;T&gt;</c> exists here, since the Process Pack
/// catalogue and each edition's default composition are static, in-memory data, not
/// rows in <c>HrisDbContext</c>. There is also no repository registration and no
/// scoped evaluator instance: <see cref="Domain.EntitlementEvaluator"/> is a static
/// class, the same "no instance, no lifetime to choose" shape
/// <see cref="Domain.ProcessPackCatalog"/> and <see cref="Domain.EditionDefaultPackComposition"/>
/// already have, since none of the three hold any state a DI container needs to
/// manage.
///
/// Called from <c>Hris.Api</c>'s <c>Program.cs</c> during the <c>AddFoundation()</c>
/// step -- this framework's own Dependencies section names no concrete
/// ProjectReference of its own, so it has no ordering requirement relative to any
/// other framework's own registration.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEntitlementFramework(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var thisAssembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(config => config.RegisterServicesFromAssembly(thisAssembly));
        services.AddValidatorsFromAssembly(thisAssembly);

        return services;
    }
}
