using System.Reflection;
using FluentValidation;
using Hris.Foundation.RulesEngine.Domain;
using Hris.Foundation.RulesEngine.Infrastructure.Persistence;
using Hris.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Hris.Foundation.RulesEngine;

/// <summary>
/// Rules Engine's single registration entry point, per module-registration.md's
/// Module Entry Point section -- the identical shape every other Sprint 3 framework's
/// own registration establishes.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRulesEngineFramework(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var thisAssembly = Assembly.GetExecutingAssembly();

        PersistenceAssemblyRegistry.Register(thisAssembly);

        services.AddMediatR(config => config.RegisterServicesFromAssembly(thisAssembly));
        services.AddValidatorsFromAssembly(thisAssembly);

        services.AddScoped<IRuleDefinitionRepository, RuleDefinitionRepository>();
        services.AddScoped<RuleEvaluator>();

        return services;
    }
}
