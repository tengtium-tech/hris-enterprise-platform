using System.Reflection;
using FluentValidation;
using Hris.Foundation.Events.Domain;
using Hris.Foundation.Events.Infrastructure.Persistence;
using Hris.Foundation.Events.Infrastructure.Publishing;
using Hris.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Hris.Foundation.Events;

/// <summary>
/// Event Framework's single registration entry point, per module-registration.md's
/// Module Entry Point section -- the identical shape
/// <c>Hris.Foundation.Identity.ServiceCollectionExtensions</c> establishes: this
/// framework defines its own <c>IEntityTypeConfiguration&lt;OutboxEntry&gt;</c> and a
/// real MediatR command/query surface, so both <see cref="PersistenceAssemblyRegistry.Register"/>
/// and <c>AddMediatR</c> are called here.
///
/// Called from <c>Hris.Api</c>'s <c>Program.cs</c> during the <c>AddFoundation()</c>
/// step, after <c>AddIdentityFramework()</c> and before <c>AddHrisInfrastructure()</c> --
/// <c>OutboxDispatcherBackgroundService</c> issues a MediatR query against
/// Configuration Framework (see that class's own remarks), the identical ordering
/// reason <c>AddIdentityFramework()</c> already documents in <c>Program.cs</c>.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEventFramework(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var thisAssembly = Assembly.GetExecutingAssembly();

        PersistenceAssemblyRegistry.Register(thisAssembly);

        services.AddMediatR(config => config.RegisterServicesFromAssembly(thisAssembly));
        services.AddValidatorsFromAssembly(thisAssembly);

        services.AddScoped<IOutboxEntryRepository, OutboxEntryRepository>();
        services.AddScoped<IEventPublisher, OutboxEventPublisher>();

        // outbox-pattern.md's own Background Publisher section: "Publishing must occur
        // outside the original business transaction" -- a hosted service, not
        // something any request-scoped command handler invokes directly.
        services.AddHostedService<OutboxDispatcherBackgroundService>();

        return services;
    }
}
