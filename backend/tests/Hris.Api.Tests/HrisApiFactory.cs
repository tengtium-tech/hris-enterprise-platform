using System.Diagnostics.CodeAnalysis;
using Hris.Application;
using Hris.Foundation.Audit;
using Hris.Foundation.Authorization;
using Hris.Foundation.Configuration;
using Hris.Foundation.Entitlement;
using Hris.Foundation.Events;
using Hris.Foundation.Extension;
using Hris.Foundation.FileStorage;
using Hris.Foundation.Identity;
using Hris.Foundation.JobProcessing;
using Hris.Foundation.Localization;
using Hris.Foundation.Logging;
using Hris.Foundation.Notification;
using Hris.Foundation.Numbering;
using Hris.Foundation.RulesEngine;
using Hris.Foundation.Scheduling;
using Hris.Foundation.Search;
using Hris.Foundation.StatutoryReferenceData;
using Hris.Foundation.Tenant;
using Hris.Foundation.Validation;
using Hris.Foundation.WorkflowEngine;
using Hris.Infrastructure;
using Hris.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace Hris.Api.Tests;

/// <summary>
/// One disposable, real PostgreSQL instance per test class -- the identical
/// <see cref="IClassFixture{TFixture}"/> lifetime and Testcontainers pattern
/// <c>PostgresContainerFixture</c> already establishes in
/// <c>Hris.Infrastructure.IntegrationTests</c>, applied here one layer up: this
/// factory boots the real <c>Hris.Api</c> host (every <c>AddXFramework()</c> call in
/// <c>Program.cs</c>, unmodified) against that database, rather than faking any part
/// of the composition root under test.
///
/// Wraps <see cref="WebApplicationFactory{TEntryPoint}"/> by composition, not
/// inheritance, for the same disposal-ordering reason this type's own git history
/// records: a type inheriting <c>WebApplicationFactory&lt;T&gt;</c> while also
/// implementing xUnit's own <see cref="IAsyncLifetime"/> gives xUnit's fixture
/// teardown two independent disposal paths into the same object.
///
/// <see cref="InitializeAsync"/> creates the database schema through a throwaway,
/// plain <see cref="ServiceProvider"/> -- mirroring <c>PostgresContainerFixture</c>'s
/// own proven-safe shape exactly -- before ever building the real
/// <see cref="WebApplicationFactory{TEntryPoint}"/>. This is not optional ceremony:
/// building the real factory starts the real ASP.NET Core generic host, which starts
/// every real <c>IHostedService</c> immediately, including
/// <c>OutboxDispatcherBackgroundService</c> (Event Framework). That background
/// service's own first iteration raced against this fixture's own schema creation
/// when both ran against the same freshly-started host -- observed directly as
/// "relation 'configuration_setting' does not exist," followed by the .NET Generic
/// Host's own default <c>BackgroundServiceExceptionBehavior.StopHost</c> policy
/// tearing down the entire host (and this fixture's own <c>TestServer</c> with it)
/// mid-test, an ASP.NET-Core-and-hosted-service interaction no prior test project in
/// this repository exercises, since none of them boot a real <c>IHost</c> -- only a
/// plain <see cref="ServiceProvider"/>, which never starts a hosted service at all.
/// Creating the schema before the real host ever starts removes the race instead of
/// racing it a second time with a retry loop.
/// </summary>
[SuppressMessage(
    "Performance",
    "CA1515:Consider making public types internal",
    Justification = "Must be public: xUnit1000 requires every IClassFixture<T> test " +
        "class to be public, and a public class cannot expose a less-accessible " +
        "constructor parameter type -- this project's own test classes take this " +
        "fixture directly. Not externally consumed.")]
public sealed class HrisApiFactory : IAsyncLifetime, IAsyncDisposable
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16")
        .WithDatabase("hris_api_tests")
        .WithUsername("hris_test")
        .WithPassword("hris_test_only")
        .Build();

    private WebApplicationFactory<Program>? _webApplicationFactory;

    public IServiceProvider Services =>
        _webApplicationFactory?.Services ?? throw new InvalidOperationException("Factory not initialized.");

    public HttpClient CreateClient() =>
        (_webApplicationFactory ?? throw new InvalidOperationException("Factory not initialized.")).CreateClient();

    public async Task InitializeAsync()
    {
        await _container.StartAsync().ConfigureAwait(false);

        await EnsureDatabaseSchemaCreatedAsync(_container.GetConnectionString()).ConfigureAwait(false);

        _webApplicationFactory = new InnerFactory(_container.GetConnectionString());

        // Touching Services here forces WebApplicationFactory to build and start the
        // real host (and every real IHostedService with it) while this method is
        // still the one xUnit is awaiting, rather than lazily on the first
        // CreateClient() call inside a test method -- a start-up failure surfaces
        // here, attributed to fixture initialization, not to whichever test happened
        // to run first.
        _ = Services;
    }

    /// <summary>
    /// Mirrors <c>PostgresContainerFixture</c>'s own registration list exactly (every
    /// <c>AddXFramework()</c> call <c>Program.cs</c> makes, in the same order) so
    /// <see cref="PersistenceAssemblyRegistry"/> sees the identical set of assemblies
    /// production does -- built through a plain <see cref="ServiceProvider"/>, never
    /// an <see cref="Microsoft.Extensions.Hosting.IHost"/>, so no
    /// <c>IHostedService</c> starts running against a database that does not have its
    /// schema yet. Update this list when <c>Program.cs</c> gains a new framework.
    /// </summary>
    private static async Task EnsureDatabaseSchemaCreatedAsync(string connectionString)
    {
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
        services.AddValidationFramework();
        services.AddLocalizationFramework();
        services.AddTenantFramework();
        services.AddExtensionFramework();
        services.AddFileStorageFramework();
        services.AddNumberingFramework();
        services.AddSearchFramework();
        services.AddSchedulingFramework();
        services.AddJobProcessingFramework();
        services.AddStatutoryReferenceDataFramework();
        services.AddWorkflowEngineFramework();
        services.AddNotificationFramework();
        services.AddEntitlementFramework();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:HrisDatabase"] = connectionString,
            })
            .Build();
        services.AddHrisInfrastructure(configuration);

        await using var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<HrisDbContext>();
        await dbContext.Database.EnsureCreatedAsync().ConfigureAwait(false);
    }

    public async Task DisposeAsync()
    {
        if (_webApplicationFactory is not null)
        {
            await _webApplicationFactory.DisposeAsync().ConfigureAwait(false);
        }

        await _container.DisposeAsync().ConfigureAwait(false);
    }

    // CA1001 ("owns a disposable field") wants this type to be genuinely
    // IAsyncDisposable, not merely IAsyncLifetime -- the two never race, since
    // nothing in this project disposes an HrisApiFactory except xUnit's own
    // IAsyncLifetime.DisposeAsync() call above.
    ValueTask IAsyncDisposable.DisposeAsync() => new(DisposeAsync());

    private sealed class InnerFactory : WebApplicationFactory<Program>
    {
        private readonly string _connectionString;

        public InnerFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            // IWebHostBuilder.UseSetting, not ConfigureAppConfiguration -- the latter
            // appends a configuration source that a minimal-hosting Program.cs
            // (top-level statements building WebApplicationBuilder directly, not the
            // older Host.CreateDefaultBuilder().ConfigureWebHostDefaults(...) shape)
            // does not reliably pick up before builder.Configuration is read;
            // UseSetting writes directly into the configuration this host actually
            // builds from.
            builder.UseSetting("ConnectionStrings:HrisDatabase", _connectionString);
        }
    }
}
