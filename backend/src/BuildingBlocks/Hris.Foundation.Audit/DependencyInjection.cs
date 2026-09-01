using System.Reflection;
using FluentValidation;
using Hris.Foundation.Audit.Application;
using Hris.Foundation.Audit.Domain;
using Hris.Foundation.Audit.Infrastructure.Persistence;
using Hris.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Hris.Foundation.Audit;

/// <summary>
/// Audit Framework's single registration entry point, per module-registration.md's
/// Module Entry Point section -- the identical shape every other Sprint 3 framework's
/// own registration establishes.
///
/// Called from <c>Hris.Api</c>'s <c>Program.cs</c> during the <c>AddFoundation()</c>
/// step, after <c>AddAuthorizationFramework()</c> and <c>AddEventFramework()</c> --
/// <c>SearchAuditRecordsQueryHandler</c>/<c>GetAuditRecordByIdQueryHandler</c> issue a
/// MediatR query against Authorization Framework, and <c>AuditRecorder</c> publishes
/// through Event Framework's own <c>IEventPublisher</c>.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAuditFramework(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var thisAssembly = Assembly.GetExecutingAssembly();

        PersistenceAssemblyRegistry.Register(thisAssembly);

        services.AddMediatR(config => config.RegisterServicesFromAssembly(thisAssembly));
        services.AddValidatorsFromAssembly(thisAssembly);

        services.AddScoped<IAuditRecordRepository, AuditRecordRepository>();
        services.AddScoped<IAuditSearchService, AuditSearchService>();
        services.AddScoped<IAuditRecorder, AuditRecorder>();

        return services;
    }
}
