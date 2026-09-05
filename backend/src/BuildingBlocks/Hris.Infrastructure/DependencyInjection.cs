using Hris.Application.Abstractions;
using Hris.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hris.Infrastructure;

/// <summary>
/// The Infrastructure composition root, per module-registration.md's Registration Flow:
/// <c>AddFoundation()</c> then <c>AddInfrastructure()</c>. Called from
/// <c>Hris.Api</c>'s <c>Program.cs</c> once every Foundation framework's own
/// <c>AddXFramework(...)</c> call has already populated
/// <see cref="PersistenceAssemblyRegistry"/> -- registering <see cref="HrisDbContext"/>
/// before every configuration assembly is known would silently build an incomplete
/// model.
///
/// The connection string key <c>ConnectionStrings:HrisDatabase</c> is this project's
/// own default, not yet specified in docs/08-devops/environment-strategy.md -- stated
/// here as a plain gap-filling default per CLAUDE.md's "do not manufacture decision
/// points... if a question has an obvious answer, answer it and say what you did,"
/// not as a claim the environment strategy document already says this.
///
/// Named <see cref="ServiceCollectionExtensions"/>, not <c>DependencyInjection</c>, per
/// CA1724 -- see <c>Hris.Application.ServiceCollectionExtensions</c>'s own remarks for
/// why; the file keeps the module-registration.md-documented name regardless.
/// </summary>
public static class ServiceCollectionExtensions
{
    public const string ConnectionStringName = "HrisDatabase";

    public static IServiceCollection AddHrisInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Missing required connection string 'ConnectionStrings:{ConnectionStringName}'. " +
                "Startup fails loudly here rather than falling back to a default, per " +
                "module-registration.md's own guidance: 'Fail startup loudly where a module's " +
                "required configuration or dependency is absent.'");

        services.AddDbContext<HrisDbContext>((_, options) =>
        {
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(HrisDbContext).Assembly.FullName));

            // naming-conventions.md's Table Naming finding: "choose one physical
            // convention... configure it once, globally, in DbContext.OnModelCreating
            // -- never per-entity." Configured here, at the one place every entity
            // configuration flows through, per that instruction -- see this project's
            // own .csproj for why EFCore.NamingConventions specifically.
            options.UseSnakeCaseNamingConvention();

            // dbcontext-design.md, "Lazy Loading": "Lazy loading is prohibited... This
            // prevents hidden database queries and N+1 performance issues." EF Core has
            // lazy loading disabled unless the lazy-loading proxies package is
            // referenced and explicitly opted into; this project references neither, so
            // the prohibition holds structurally rather than by convention alone.
            options.EnableDetailedErrors();
        });

        // TransactionBehavior<TRequest,TResponse> (Hris.Application) constructor-
        // injects IUnitOfWork for every command in the platform -- AddDbContext above
        // registers HrisDbContext itself (scoped) but never its own IUnitOfWork
        // interface, since EF Core's own registration never infers implemented
        // interfaces from a concrete DbContext type. A genuine pre-existing gap, found
        // by Sprint 7's own Hris.Api.Tests -- the first test in this repository's own
        // history to dispatch anything through the real, fully-wired DI container
        // rather than a framework's own fake-repository unit test or a read-only LINQ
        // translation check, both of which bypass this pipeline behavior entirely.
        // Resolved from the same scoped HrisDbContext instance the request's own
        // handlers already use, never a second DbContext instance racing the first.
        services.AddScoped<IUnitOfWork>(serviceProvider => serviceProvider.GetRequiredService<HrisDbContext>());

        return services;
    }
}
