using System.Reflection;
using FluentValidation;
using Hris.Infrastructure.Persistence;
using Hris.Modules.Workflow.Domain;
using Hris.Modules.Workflow.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Hris.Modules.Workflow;

/// <summary>
/// Workflow module's single registration entry point, per module-registration.md's
/// Module Entry Point section, the identical shape every prior module establishes.
/// No compile-time dependency on any sibling module or Phase 1 framework, including
/// the Workflow Engine this module's definitions are executed by, per this
/// platform's own standing "reference by identifier, never by ProjectReference"
/// rule.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWorkflowModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var thisAssembly = Assembly.GetExecutingAssembly();

        PersistenceAssemblyRegistry.Register(thisAssembly);

        services.AddMediatR(config => config.RegisterServicesFromAssembly(thisAssembly));
        services.AddValidatorsFromAssembly(thisAssembly);

        services.AddScoped<IWorkflowDefinitionRepository, WorkflowDefinitionRepository>();
        services.AddScoped<IApprovalPolicyRepository, ApprovalPolicyRepository>();
        services.AddScoped<IApprovalDelegationRepository, ApprovalDelegationRepository>();

        return services;
    }
}
