using FluentAssertions;
using Hris.Infrastructure.Persistence;
using Hris.Modules.Workflow.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Hris.Modules.Workflow.Tests.Infrastructure.Persistence;

/// <summary>
/// Forces EF Core's own model validation for this module's own
/// <c>IEntityTypeConfiguration&lt;T&gt;</c> classes against a real Npgsql provider,
/// without ever opening a connection: accessing <see cref="DbContext.Model"/>
/// triggers full model building.
///
/// This module pushes owned-type nesting one level deeper than any prior module has:
/// <c>WorkflowDefinition</c> owns a step collection, each step owns an escalation
/// override, and that override owns its own approver resolution rule. Whether EF
/// Core accepts that depth is exactly the kind of question this smoke test exists to
/// answer early, and it is the reason this test is written before the Application
/// layer rather than after.
/// </summary>
public sealed class WorkflowPersistenceModelTests
{
    public WorkflowPersistenceModelTests()
    {
        PersistenceAssemblyRegistry.Register(typeof(ServiceCollectionExtensions).Assembly);
    }

    [Fact]
    public void HrisDbContext_BuildsItsModel_WithoutThrowing_WhenWorkflowModuleIsRegistered()
    {
        using var dbContext = CreateDbContext();

        var act = () => dbContext.Model;

        act.Should().NotThrow();
    }

    [Fact]
    public void HrisDbContext_Model_IncludesEveryAggregateRoot()
    {
        using var dbContext = CreateDbContext();

        var entityTypes = dbContext.Model.GetEntityTypes().Select(t => t.ClrType).ToList();

        entityTypes.Should().Contain(typeof(WorkflowDefinition));
        entityTypes.Should().Contain(typeof(ApprovalPolicy));
        entityTypes.Should().Contain(typeof(ApprovalDelegation));
    }

    [Fact]
    public void HrisDbContext_Model_MapsWorkflowStepAsAnOwnedEntity_NotAnAggregateRoot()
    {
        using var dbContext = CreateDbContext();

        var stepType = dbContext.Model.GetEntityTypes().Single(t => t.ClrType == typeof(WorkflowStep));

        stepType.IsOwned().Should().BeTrue();
    }

    private static HrisDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<HrisDbContext>()
            .UseNpgsql("Host=localhost;Database=model_validation_only;Username=none;Password=none")
            .Options;

        return new HrisDbContext(options);
    }
}
