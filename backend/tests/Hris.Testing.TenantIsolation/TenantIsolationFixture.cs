using System.Diagnostics.CodeAnalysis;
using Hris.Application;
using Hris.Foundation.Audit;
using Hris.Foundation.Authorization;
using Hris.Foundation.Configuration;
using Hris.Foundation.DocumentManagement;
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
using Hris.Modules.Administration;
using Hris.Modules.Employee;
using Hris.Modules.Employment;
using Hris.Modules.Organization;
using Hris.Modules.Position;
using Hris.Modules.Timekeeping;
using Hris.Modules.Workflow;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace Hris.Testing.TenantIsolation;

/// <summary>
/// One disposable, real PostgreSQL instance per test class, modeled on
/// <see cref="Hris.Infrastructure.IntegrationTests.PostgresContainerFixture"/>
/// and extended with every merged module and every foundation framework so that
/// <see cref="PersistenceAssemblyRegistry"/> sees the same assembly set production does.
/// Two canonical tenants are available via <see cref="TenantIdentifiers"/>.
/// </summary>
[SuppressMessage(
    "Performance",
    "CA1515:Consider making public types internal",
    Justification = "Must be public: xUnit1000 requires every IClassFixture<T> test "
        + "class to be public, and a public class cannot expose a less-accessible "
        + "fixture parameter type.")]
public sealed class TenantIsolationFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private ServiceProvider? _serviceProvider;

    public AsyncServiceScope CreateScope() =>
        (_serviceProvider ?? throw new InvalidOperationException("Fixture not initialized.")).CreateAsyncScope();

    public async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder("postgres:16")
            .WithDatabase("hris_tenant_isolation_tests")
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

        services.AddOrganizationModule();
        services.AddPositionModule();
        services.AddEmploymentModule();
        services.AddEmployeeModule();
        services.AddAdministrationModule();
        services.AddWorkflowModule();
        services.AddTimekeepingModule();

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
