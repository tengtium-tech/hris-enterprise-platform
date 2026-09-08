using FluentAssertions;
using Hris.Infrastructure.Persistence;
using Hris.Modules.Position.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Hris.Modules.Position.Tests.Infrastructure.Persistence;

/// <summary>
/// Forces EF Core's own model validation for this module's four flat Aggregate Root
/// configurations against a real Npgsql provider, without ever opening a connection --
/// the same technique <c>OrganizationPersistenceModelTests</c> already establishes,
/// which caught two genuine EF Core constructor-binding bugs while building that
/// module (see feedback memory feedback-ef-core-constructor-binding). This module has
/// no owned navigations at all (every organizational/workforce-classification/
/// reporting reference is a plain scalar Guid), so the risk surface here is smaller,
/// but the check is still cheap and worth running.
/// </summary>
public sealed class PositionPersistenceModelTests
{
    public PositionPersistenceModelTests()
    {
        PersistenceAssemblyRegistry.Register(typeof(ServiceCollectionExtensions).Assembly);
    }

    [Fact]
    public void HrisDbContext_BuildsItsModel_WithoutThrowing_WhenPositionModuleIsRegistered()
    {
        using var dbContext = CreateDbContext();

        var act = () => dbContext.Model;

        act.Should().NotThrow();
    }

    [Fact]
    public void HrisDbContext_Model_IncludesEveryPositionModuleAggregateRoot()
    {
        using var dbContext = CreateDbContext();

        var entityTypes = dbContext.Model.GetEntityTypes().Select(t => t.ClrType).ToList();

        entityTypes.Should().Contain(typeof(Hris.Modules.Position.Domain.Position));
        entityTypes.Should().Contain(typeof(JobFamily));
        entityTypes.Should().Contain(typeof(JobClassification));
        entityTypes.Should().Contain(typeof(JobGrade));
    }

    private static HrisDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<HrisDbContext>()
            .UseNpgsql("Host=localhost;Database=model_validation_only;Username=none;Password=none")
            .Options;

        return new HrisDbContext(options);
    }
}
