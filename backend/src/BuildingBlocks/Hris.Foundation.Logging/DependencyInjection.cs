using Hris.Foundation.Logging.Application;
using Hris.Foundation.Logging.Domain;
using Hris.Foundation.Logging.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Hris.Foundation.Logging;

/// <summary>
/// Logging Framework's single registration entry point, per module-registration.md's
/// Module Entry Point section -- the same one-extension-method convention
/// Configuration Framework's own <see cref="ServiceCollectionExtensions"/> follows.
///
/// Unlike Configuration Framework's registration, this does not call
/// <c>PersistenceAssemblyRegistry.Register</c> or <c>AddMediatR</c> for this
/// framework's own assembly: Logging Framework defines no
/// <c>IEntityTypeConfiguration</c> (it persists nothing through <c>HrisDbContext</c> --
/// <see cref="ILogSink"/> writes to Serilog, not PostgreSQL) and no MediatR requests of
/// its own (see <see cref="ILoggingService"/>'s own remarks for why). It still depends
/// on MediatR's container registration existing, to resolve <c>ISender</c> for the one
/// query it issues against Configuration Framework -- <c>AddConfigurationFramework()</c>
/// (or any other framework's own <c>AddMediatR</c> call) satisfies that; this method
/// does not call <c>AddMediatR</c> itself to avoid a second, redundant MediatR
/// container registration for the same assembly.
///
/// Serilog's own <c>Serilog.ILogger</c> registration is not performed here either --
/// it comes from <c>Hris.Api</c>'s own <c>UseSerilog()</c> host bootstrap (see
/// <c>Program.cs</c>), the same "compose in the host, not in the framework" split
/// <c>AddHrisInfrastructure</c> follows for its own <c>UseNpgsql</c> connection string.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLoggingFramework(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Singleton, not Scoped like Configuration Framework's own repository
        // registrations: neither type here holds a per-request resource the way
        // ConfigurationSettingRepository holds HrisDbContext. Serilog's own ILogger is
        // itself meant to be a long-lived singleton, and LoggingService's own MediatR
        // ISender dependency is resolved fresh per call internally by MediatR rather
        // than captured at construction, so it is safe for a singleton to hold one.
        services.AddSingleton<ILogSink, SerilogLogSink>();
        services.AddSingleton<ILoggingService, LoggingService>();

        return services;
    }
}
