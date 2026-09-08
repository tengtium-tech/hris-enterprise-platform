using FluentAssertions;
using Hris.Infrastructure.Persistence;
using Hris.Modules.Employment.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Hris.Modules.Employment.Tests.Infrastructure.Persistence;

/// <summary>
/// Forces EF Core's own model validation for this module's own
/// <c>IEntityTypeConfiguration&lt;T&gt;</c> classes against a real Npgsql provider,
/// without ever opening a connection: accessing <see cref="DbContext.Model"/>
/// triggers full model building, including the three owned-collection Aggregate
/// Roots (Employment, EmploymentContract, EmploymentAssignment) and their nested
/// owned Value Objects (CompensationAmount, ContractPeriod). This sandbox has no
/// Docker, so this is the closest verification available in this environment, and
/// it is exactly the shape of check most likely to catch a real modeling mistake --
/// a wrong <c>HasForeignKey</c> target, a missing owned-type callback, or a
/// constructor-binding conflict all throw at model-build time, not at query time.
/// </summary>
public sealed class EmploymentPersistenceModelTests
{
    public EmploymentPersistenceModelTests()
    {
        PersistenceAssemblyRegistry.Register(typeof(ServiceCollectionExtensions).Assembly);
    }

    [Fact]
    public void HrisDbContext_BuildsItsModel_WithoutThrowing_WhenEmploymentModuleIsRegistered()
    {
        using var dbContext = CreateDbContext();

        var act = () => dbContext.Model;

        act.Should().NotThrow();
    }

    [Fact]
    public void HrisDbContext_Model_IncludesEveryAggregateRoot()
    {
        using var dbContext = CreateDbContext();

        var entityTypeNames = dbContext.Model.GetEntityTypes().Select(t => t.ClrType).ToList();

        entityTypeNames.Should().Contain(typeof(Hris.Modules.Employment.Domain.Employment));
        entityTypeNames.Should().Contain(typeof(EmploymentContract));
        entityTypeNames.Should().Contain(typeof(EmploymentAssignment));
    }

    private static HrisDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<HrisDbContext>()
            .UseNpgsql("Host=localhost;Database=model_validation_only;Username=none;Password=none")
            .Options;

        return new HrisDbContext(options);
    }
}
