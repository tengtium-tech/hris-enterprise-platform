using System.Diagnostics.CodeAnalysis;
using Hris.Foundation.JobProcessing.Domain;
using Hris.Modules.Administration.Application.Queries;
using Hris.Modules.Administration.Domain;
using Hris.Modules.Employee.Application.Queries;
using Hris.Modules.Employee.Domain;
using Hris.Modules.Employment.Application.Queries;
using Hris.Modules.Employment.Domain;
using Hris.Modules.Organization.Application.Queries;
using Hris.Modules.Organization.Domain;
using Hris.Modules.Position.Application.Queries;
using Hris.Modules.Position.Domain;
using Hris.Modules.Timekeeping.Application.Queries;
using Hris.Modules.Timekeeping.Domain;
using Hris.Modules.Workflow.Application.Queries;
using Hris.Modules.Workflow.Domain;
using Hris.Testing.TenantIsolation;
using MediatR;
using Xunit;
using FluentAssertions;

namespace Hris.CriticalRequirements.Tests;

/// <summary>
/// CTR-ISO-001 through CTR-ISO-004, docs/09-testing/critical-test-requirements.md §5.
/// Exercises every merged module with tenant-scoped data against the shared
/// <see cref="TenantIsolationFixture"/> to prove isolation is real, not mocked.
/// </summary>
[SuppressMessage(
    "Performance",
    "CA1515:Consider making public types internal",
    Justification = "xUnit1000 requires public IClassFixture<T> test classes.")]
public sealed class TenantIsolationTests : TenantIsolationTestBase
{
    public TenantIsolationTests(TenantIsolationFixture fixture) : base(fixture) { }

    public static IEnumerable<object[]> TenantScopedModules() =>
        new[]
        {
            new object[] { "Organization" },
            new object[] { "Position" },
            new object[] { "Employment" },
            new object[] { "Employee" },
            new object[] { "Administration" },
            new object[] { "Workflow" },
            new object[] { "Timekeeping" },
        };

    [Theory]
    [MemberData(nameof(TenantScopedModules))]
    public async Task CTR_ISO_001_NoCrossTenantRead(string moduleName)
    {
        var ct = CancellationToken.None;
        var tenantA = TenantIdentifiers.TenantAId;
        var tenantB = TenantIdentifiers.TenantBId;

        switch (moduleName)
        {
            case "Organization":
                {
                    var id = new OrganizationId(Guid.NewGuid());
                    var created = Organization.Create(id, tenantA, "Acme Corp", "ACME", null, null, DateTimeOffset.UtcNow).Value;
                    await SeedAsync(created, ct);
                    var repo = GetService<IOrganizationRepository>();
                    var listB = await repo.ListByTenantAsync(tenantB, ct);
                    listB.Should().BeEmpty($"Tenant B should not see Tenant A's {moduleName}");
                    var listA = await repo.ListByTenantAsync(tenantA, ct);
                    listA.Should().ContainSingle(o => o.Id == id);
                    break;
                }
            case "Position":
                {
                    var id = new PositionId(Guid.NewGuid());
                    var created = Position.Create(
                        id, tenantA, "P-001", "Engineer", "Permanent", Guid.NewGuid(),
                        null, null, null, null, null, null, null, null,
                        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, 1,
                        DateTimeOffset.UtcNow).Value;
                    await SeedAsync(created, ct);
                    var repo = GetService<IPositionRepository>();
                    var listB = await repo.ListByTenantAsync(tenantB, ct);
                    listB.Should().BeEmpty($"Tenant B should not see Tenant A's {moduleName}");
                    var listA = await repo.ListByTenantAsync(tenantA, ct);
                    listA.Should().ContainSingle(p => p.Id == id);
                    break;
                }
            case "Employment":
                {
                    var id = new EmploymentId(Guid.NewGuid());
                    var created = Employment.Create(
                        id, tenantA, Guid.NewGuid(), "EMP-001", "Regular", "Permanent",
                        true, null, null, false, false, DateTimeOffset.UtcNow).Value;
                    await SeedAsync(created, ct);
                    var repo = GetService<IEmploymentRepository>();
                    var listB = await repo.ListByTenantAsync(tenantB, ct);
                    listB.Should().BeEmpty($"Tenant B should not see Tenant A's {moduleName}");
                    var listA = await repo.ListByTenantAsync(tenantA, ct);
                    listA.Should().ContainSingle(e => e.Id == id);
                    break;
                }
            case "Employee":
                {
                    var id = new EmployeeId(Guid.NewGuid());
                    var created = Employee.Create(
                        id, tenantA, "EMP-001", "Jane", null, "Doe", null, null, null,
                        new DateOnly(1990, 1, 1), "Manila", Gender.Female, CivilStatus.Single,
                        "Filipino", "Philippines", DateTimeOffset.UtcNow).Value;
                    await SeedAsync(created, ct);
                    var repo = GetService<IEmployeeRepository>();
                    var listB = await repo.ListByTenantAsync(tenantB, ct);
                    listB.Should().BeEmpty($"Tenant B should not see Tenant A's {moduleName}");
                    var listA = await repo.ListByTenantAsync(tenantA, ct);
                    listA.Should().ContainSingle(e => e.Id == id);
                    break;
                }
            case "Administration":
                {
                    var id = new UserAccountId(Guid.NewGuid());
                    var created = UserAccount.Create(
                        id, tenantA, AccountType.External, null,
                        DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)), null,
                        Guid.NewGuid(), DateTimeOffset.UtcNow).Value;
                    await SeedAsync(created, ct);
                    var repo = GetService<IUserAccountRepository>();
                    var listB = await repo.ListByTenantAsync(tenantB, ct);
                    listB.Should().BeEmpty($"Tenant B should not see Tenant A's {moduleName}");
                    var listA = await repo.ListByTenantAsync(tenantA, ct);
                    listA.Should().ContainSingle(a => a.Id == id);
                    break;
                }
            case "Workflow":
                {
                    var id = new WorkflowDefinitionId(Guid.NewGuid());
                    var created = WorkflowDefinition.Author(
                        id, tenantA, Guid.NewGuid(), "Onboarding", null, null, false,
                        Guid.NewGuid(), DateTimeOffset.UtcNow).Value;
                    await SeedAsync(created, ct);
                    var repo = GetService<IWorkflowDefinitionRepository>();
                    var listB = await repo.ListByTenantAsync(tenantB, ct);
                    listB.Should().BeEmpty($"Tenant B should not see Tenant A's {moduleName}");
                    var listA = await repo.ListByTenantAsync(tenantA, ct);
                    listA.Should().ContainSingle(d => d.Id == id);
                    break;
                }
            case "Timekeeping":
                {
                    var id = new WorkScheduleId(Guid.NewGuid());
                    var created = WorkSchedule.Create(
                        id, tenantA, "Standard", null,
                        new List<DayOfWeek>
                        {
                            DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
                            DayOfWeek.Thursday, DayOfWeek.Friday,
                        },
                        null, null, new DateOnly(2026, 1, 1), Guid.NewGuid(),
                        DateTimeOffset.UtcNow).Value;
                    await SeedAsync(created, ct);
                    var repo = GetService<IWorkScheduleRepository>();
                    var listB = await repo.ListByTenantAsync(tenantB, ct);
                    listB.Should().BeEmpty($"Tenant B should not see Tenant A's {moduleName}");
                    var listA = await repo.ListByTenantAsync(tenantA, ct);
                    listA.Should().ContainSingle(s => s.Id == id);
                    break;
                }
            default:
                throw new ArgumentOutOfRangeException(nameof(moduleName), moduleName, "Unknown module.");
        }
    }

    [Theory]
    [MemberData(nameof(TenantScopedModules))]
    public async Task CTR_ISO_002_IdentifierEnumerationDoesNotCrossTenants(string moduleName)
    {
        var ct = CancellationToken.None;
        var tenantA = TenantIdentifiers.TenantAId;
        var tenantB = TenantIdentifiers.TenantBId;
        var mediator = GetService<IMediator>();

        switch (moduleName)
        {
            case "Organization":
                {
                    var id = new OrganizationId(Guid.NewGuid());
                    var created = Organization.Create(id, tenantA, "Acme Corp", "ACME", null, null, DateTimeOffset.UtcNow).Value;
                    await SeedAsync(created, ct);
                    var result = await mediator.Send(new GetOrganizationQuery(id.Value, tenantB), ct);
                    result.IsFailure.Should().BeTrue($"Cross-tenant {moduleName} access must yield NotFound, not success");
                    break;
                }
            case "Position":
                {
                    var id = new PositionId(Guid.NewGuid());
                    var created = Position.Create(
                        id, tenantA, "P-001", "Engineer", "Permanent", Guid.NewGuid(),
                        null, null, null, null, null, null, null, null,
                        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, 1,
                        DateTimeOffset.UtcNow).Value;
                    await SeedAsync(created, ct);
                    var result = await mediator.Send(new GetPositionQuery(id.Value, tenantB), ct);
                    result.IsFailure.Should().BeTrue($"Cross-tenant {moduleName} access must yield NotFound, not success");
                    break;
                }
            case "Employment":
                {
                    var id = new EmploymentId(Guid.NewGuid());
                    var created = Employment.Create(
                        id, tenantA, Guid.NewGuid(), "EMP-001", "Regular", "Permanent",
                        true, null, null, false, false, DateTimeOffset.UtcNow).Value;
                    await SeedAsync(created, ct);
                    var result = await mediator.Send(new GetEmploymentQuery(id.Value, tenantB), ct);
                    result.IsFailure.Should().BeTrue($"Cross-tenant {moduleName} access must yield NotFound, not success");
                    break;
                }
            case "Employee":
                {
                    var id = new EmployeeId(Guid.NewGuid());
                    var created = Employee.Create(
                        id, tenantA, "EMP-001", "Jane", null, "Doe", null, null, null,
                        new DateOnly(1990, 1, 1), "Manila", Gender.Female, CivilStatus.Single,
                        "Filipino", "Philippines", DateTimeOffset.UtcNow).Value;
                    await SeedAsync(created, ct);
                    var result = await mediator.Send(new GetEmployeeQuery(id.Value, tenantB), ct);
                    result.IsFailure.Should().BeTrue($"Cross-tenant {moduleName} access must yield NotFound, not success");
                    break;
                }
            case "Administration":
                {
                    var id = new UserAccountId(Guid.NewGuid());
                    var created = UserAccount.Create(
                        id, tenantA, AccountType.External, null,
                        DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)), null,
                        Guid.NewGuid(), DateTimeOffset.UtcNow).Value;
                    await SeedAsync(created, ct);
                    var result = await mediator.Send(new GetUserAccountQuery(id.Value, tenantB), ct);
                    result.IsFailure.Should().BeTrue($"Cross-tenant {moduleName} access must yield NotFound, not success");
                    break;
                }
            case "Workflow":
                {
                    var id = new WorkflowDefinitionId(Guid.NewGuid());
                    var created = WorkflowDefinition.Author(
                        id, tenantA, Guid.NewGuid(), "Onboarding", null, null, false,
                        Guid.NewGuid(), DateTimeOffset.UtcNow).Value;
                    await SeedAsync(created, ct);
                    var result = await mediator.Send(new GetWorkflowDefinitionByIdQuery(id.Value, tenantB), ct);
                    result.IsFailure.Should().BeTrue($"Cross-tenant {moduleName} access must yield NotFound, not success");
                    break;
                }
            case "Timekeeping":
                {
                    var id = new WorkScheduleId(Guid.NewGuid());
                    var created = WorkSchedule.Create(
                        id, tenantA, "Standard", null,
                        new List<DayOfWeek>
                        {
                            DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
                            DayOfWeek.Thursday, DayOfWeek.Friday,
                        },
                        null, null, new DateOnly(2026, 1, 1), Guid.NewGuid(),
                        DateTimeOffset.UtcNow).Value;
                    await SeedAsync(created, ct);
                    var result = await mediator.Send(new GetWorkScheduleQuery(id.Value, tenantB), ct);
                    result.IsFailure.Should().BeTrue($"Cross-tenant {moduleName} access must yield NotFound, not success");
                    break;
                }
            default:
                throw new ArgumentOutOfRangeException(nameof(moduleName), moduleName, "Unknown module.");
        }
    }

    [Theory]
    [MemberData(nameof(TenantScopedModules))]
    public async Task CTR_ISO_003_IsolationEnforcedBelowTheApplicationLayer(string moduleName)
    {
        var ct = CancellationToken.None;
        var tenantA = TenantIdentifiers.TenantAId;
        var tenantB = TenantIdentifiers.TenantBId;

        switch (moduleName)
        {
            case "Organization":
                {
                    var id = new OrganizationId(Guid.NewGuid());
                    var created = Organization.Create(id, tenantA, "Acme Corp", "ACME", null, null, DateTimeOffset.UtcNow).Value;
                    await SeedAsync(created, ct);
                    var repo = GetService<IOrganizationRepository>();
                    var byId = await repo.GetByIdAsync(id, ct);
                    byId.Should().NotBeNull("GetByIdAsync has no tenant filter -- it returns the entity");
                    var listB = await repo.ListByTenantAsync(tenantB, ct);
                    listB.Should().BeEmpty("ListByTenantAsync filters below the application layer");
                    var existsB = await repo.ExistsWithCodeAsync(tenantB, "ACME", null, ct);
                    existsB.Should().BeFalse("Exists* queries filter below the application layer");
                    break;
                }
            case "Position":
                {
                    var id = new PositionId(Guid.NewGuid());
                    var created = Position.Create(
                        id, tenantA, "P-001", "Engineer", "Permanent", Guid.NewGuid(),
                        null, null, null, null, null, null, null, null,
                        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, 1,
                        DateTimeOffset.UtcNow).Value;
                    await SeedAsync(created, ct);
                    var repo = GetService<IPositionRepository>();
                    var byId = await repo.GetByIdAsync(id, ct);
                    byId.Should().NotBeNull("GetByIdAsync has no tenant filter -- it returns the entity");
                    var listB = await repo.ListByTenantAsync(tenantB, ct);
                    listB.Should().BeEmpty("ListByTenantAsync filters below the application layer");
                    var existsB = await repo.ExistsWithNumberAsync(tenantB, "P-001", null, ct);
                    existsB.Should().BeFalse("Exists* queries filter below the application layer");
                    break;
                }
            case "Employment":
                {
                    var id = new EmploymentId(Guid.NewGuid());
                    var created = Employment.Create(
                        id, tenantA, Guid.NewGuid(), "EMP-001", "Regular", "Permanent",
                        true, null, null, false, false, DateTimeOffset.UtcNow).Value;
                    await SeedAsync(created, ct);
                    var repo = GetService<IEmploymentRepository>();
                    var byId = await repo.GetByIdAsync(id, ct);
                    byId.Should().NotBeNull("GetByIdAsync has no tenant filter -- it returns the entity");
                    var listB = await repo.ListByTenantAsync(tenantB, ct);
                    listB.Should().BeEmpty("ListByTenantAsync filters below the application layer");
                    var existsB = await repo.ExistsWithNumberAsync(tenantB, "EMP-001", null, ct);
                    existsB.Should().BeFalse("Exists* queries filter below the application layer");
                    break;
                }
            case "Employee":
                {
                    var id = new EmployeeId(Guid.NewGuid());
                    var created = Employee.Create(
                        id, tenantA, "EMP-001", "Jane", null, "Doe", null, null, null,
                        new DateOnly(1990, 1, 1), "Manila", Gender.Female, CivilStatus.Single,
                        "Filipino", "Philippines", DateTimeOffset.UtcNow).Value;
                    await SeedAsync(created, ct);
                    var repo = GetService<IEmployeeRepository>();
                    var byId = await repo.GetByIdAsync(id, ct);
                    byId.Should().NotBeNull("GetByIdAsync has no tenant filter -- it returns the entity");
                    var listB = await repo.ListByTenantAsync(tenantB, ct);
                    listB.Should().BeEmpty("ListByTenantAsync filters below the application layer");
                    var existsB = await repo.ExistsWithNumberAsync(tenantB, "EMP-001", null, ct);
                    existsB.Should().BeFalse("Exists* queries filter below the application layer");
                    break;
                }
            case "Administration":
                {
                    var id = new UserAccountId(Guid.NewGuid());
                    var created = UserAccount.Create(
                        id, tenantA, AccountType.External, null,
                        DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)), null,
                        Guid.NewGuid(), DateTimeOffset.UtcNow).Value;
                    await SeedAsync(created, ct);
                    var repo = GetService<IUserAccountRepository>();
                    var byId = await repo.GetByIdAsync(id, ct);
                    byId.Should().NotBeNull("GetByIdAsync has no tenant filter -- it returns the entity");
                    var listB = await repo.ListByTenantAsync(tenantB, ct);
                    listB.Should().BeEmpty("ListByTenantAsync filters below the application layer");
                    break;
                }
            case "Workflow":
                {
                    var id = new WorkflowDefinitionId(Guid.NewGuid());
                    var created = WorkflowDefinition.Author(
                        id, tenantA, Guid.NewGuid(), "Onboarding", null, null, false,
                        Guid.NewGuid(), DateTimeOffset.UtcNow).Value;
                    await SeedAsync(created, ct);
                    var repo = GetService<IWorkflowDefinitionRepository>();
                    var byId = await repo.GetByIdAsync(id, ct);
                    byId.Should().NotBeNull("GetByIdAsync has no tenant filter -- it returns the entity");
                    var listB = await repo.ListByTenantAsync(tenantB, ct);
                    listB.Should().BeEmpty("ListByTenantAsync filters below the application layer");
                    break;
                }
            case "Timekeeping":
                {
                    var id = new WorkScheduleId(Guid.NewGuid());
                    var created = WorkSchedule.Create(
                        id, tenantA, "Standard", null,
                        new List<DayOfWeek>
                        {
                            DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
                            DayOfWeek.Thursday, DayOfWeek.Friday,
                        },
                        null, null, new DateOnly(2026, 1, 1), Guid.NewGuid(),
                        DateTimeOffset.UtcNow).Value;
                    await SeedAsync(created, ct);
                    var repo = GetService<IWorkScheduleRepository>();
                    var byId = await repo.GetByIdAsync(id, ct);
                    byId.Should().NotBeNull("GetByIdAsync has no tenant filter -- it returns the entity");
                    var listB = await repo.ListByTenantAsync(tenantB, ct);
                    listB.Should().BeEmpty("ListByTenantAsync filters below the application layer");
                    break;
                }
            default:
                throw new ArgumentOutOfRangeException(nameof(moduleName), moduleName, "Unknown module.");
        }
    }

    [Fact]
    public async Task CTR_ISO_004_BackgroundJobsCarryTenantContext()
    {
        var ct = CancellationToken.None;
        var tenantA = TenantIdentifiers.TenantAId;
        var tenantB = TenantIdentifiers.TenantBId;

        var queueId = new JobQueueId(Guid.NewGuid());
        var job = Job.Submit(
            tenantA,
            "TestJob",
            queueId,
            JobPriority.Normal,
            payloadReference: null,
            submittedByUserId: null,
            maxRetries: 3,
            DateTimeOffset.UtcNow).Value;

        await SeedAsync(job, ct);

        var repo = GetService<IJobRepository>();

        var listB = await repo.ListByQueueAsync(queueId, tenantB, maxResults: 10, ct);
        listB.Should().BeEmpty("Tenant B must not see Tenant A's background jobs");

        var listA = await repo.ListByQueueAsync(queueId, tenantA, maxResults: 10, ct);
        listA.Should().ContainSingle(j => j.Id == job.Id);
    }
}
