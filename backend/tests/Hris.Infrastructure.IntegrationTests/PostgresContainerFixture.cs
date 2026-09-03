using System.Diagnostics.CodeAnalysis;
using Hris.Application;
using Hris.Foundation.Audit;
using Hris.Foundation.Authorization;
using Hris.Foundation.Configuration;
using Hris.Foundation.Events;
using Hris.Foundation.Identity;
using Hris.Foundation.Localization;
using Hris.Foundation.Logging;
using Hris.Foundation.Numbering;
using Hris.Foundation.RulesEngine;
using Hris.Infrastructure;
using Hris.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace Hris.Infrastructure.IntegrationTests;

/// <summary>
/// One disposable, real PostgreSQL instance per test class (xUnit's own
/// <see cref="IClassFixture{TFixture}"/> lifetime -- started once in
/// <see cref="InitializeAsync"/>, torn down once in <see cref="DisposeAsync"/>),
/// per docs/09-testing/unit-and-integration-testing.md 3.1's own required
/// mechanism: "a real PostgreSQL instance provided by Testcontainers." No
/// docker-compose dependency and no fixed port -- Testcontainers assigns an
/// ephemeral host port per run, so this runs unmodified in CI (which provisions and
/// discards its own container per job, per ci-cd-pipeline.md) and on any
/// contributor's own machine without a manual setup step.
///
/// Registers every Sprint 3 Core Kernel framework except Validation (which persists
/// nothing of its own -- see that framework's own csproj header), plus Numbering
/// Framework (the one Sprint 4 framework this project's own tests actually exercise,
/// for <c>NumberSeriesConcurrencyTests</c>) -- the same bootstrap-order registration
/// list <c>Hris.Api</c>'s own <c>Program.cs</c> uses for everything up to that point.
/// <see cref="PersistenceAssemblyRegistry"/> only ever sees the assemblies an
/// <c>AddXFramework()</c> call here registers, so <see cref="HrisDbContext"/>'s own
/// model here matches production's, not a partial subset that could hide a mapping
/// conflict between two frameworks that only surfaces once both are registered
/// together.
/// </summary>
[SuppressMessage(
    "Performance",
    "CA1515:Consider making public types internal",
    Justification = "Must be public: xUnit1000 requires every IClassFixture<T> test "
        + "class to be public, and a public class cannot expose a less-accessible "
        + "constructor parameter type -- this project's own test classes take this "
        + "fixture directly. Not externally consumed; the visibility is forced by "
        + "xUnit's own fixture-sharing mechanism, not by any cross-assembly API "
        + "surface.")]
public sealed class PostgresContainerFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private ServiceProvider? _serviceProvider;

    public IServiceScope CreateScope() =>
        (_serviceProvider ?? throw new InvalidOperationException("Fixture not initialized.")).CreateScope();

    public async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder("postgres:16")
            .WithDatabase("hris_integration_tests")
            .WithUsername("hris_test")
            .WithPassword("hris_test_only")
            .Build();

        await _container.StartAsync().ConfigureAwait(false);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:HrisDatabase"] = _container.GetConnectionString(),
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton(TimeProvider.System);
        services.AddHrisApplicationBehaviors();
        services.AddConfigurationFramework();
        services.AddLoggingFramework();
        services.AddIdentityFramework();
        services.AddEventFramework();
        services.AddAuthorizationFramework();
        services.AddAuditFramework();
        services.AddRulesEngineFramework();
        services.AddLocalizationFramework();
        services.AddNumberingFramework();
        services.AddHrisInfrastructure(configuration);

        _serviceProvider = services.BuildServiceProvider();

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<HrisDbContext>();
        await dbContext.Database.EnsureCreatedAsync().ConfigureAwait(false);
    }

    public async Task DisposeAsync()
    {
        if (_serviceProvider is not null)
        {
            await _serviceProvider.DisposeAsync().ConfigureAwait(false);
        }

        if (_container is not null)
        {
            await _container.DisposeAsync().ConfigureAwait(false);
        }
    }
}
