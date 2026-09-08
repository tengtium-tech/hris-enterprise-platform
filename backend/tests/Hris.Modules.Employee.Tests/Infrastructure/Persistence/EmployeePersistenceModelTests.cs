using FluentAssertions;
using Hris.Infrastructure.Persistence;
using Hris.Modules.Employee.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Hris.Modules.Employee.Tests.Infrastructure.Persistence;

/// <summary>
/// Forces EF Core's own model validation for this module's own
/// <c>IEntityTypeConfiguration&lt;T&gt;</c> classes against a real Npgsql provider,
/// without ever opening a connection: accessing <see cref="DbContext.Model"/>
/// triggers full model building, including the two owned-collection Aggregate
/// Roots (Employee, EmployeeHistory) and their nested owned Value Objects
/// (PersonName, Address, BankAccount). This sandbox has no Docker, so this is the
/// closest verification available in this environment.
/// </summary>
public sealed class EmployeePersistenceModelTests
{
    public EmployeePersistenceModelTests()
    {
        PersistenceAssemblyRegistry.Register(typeof(ServiceCollectionExtensions).Assembly);
    }

    [Fact]
    public void HrisDbContext_BuildsItsModel_WithoutThrowing_WhenEmployeeModuleIsRegistered()
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

        entityTypeNames.Should().Contain(typeof(Hris.Modules.Employee.Domain.Employee));
        entityTypeNames.Should().Contain(typeof(EmployeeHistory));
    }

    private static HrisDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<HrisDbContext>()
            .UseNpgsql("Host=localhost;Database=model_validation_only;Username=none;Password=none")
            .Options;

        return new HrisDbContext(options);
    }
}
