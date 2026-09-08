using FluentAssertions;
using Hris.Infrastructure.Persistence;
using Hris.Modules.Administration.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Hris.Modules.Administration.Tests.Infrastructure.Persistence;

/// <summary>
/// Forces EF Core's own model validation for this module's own
/// <c>IEntityTypeConfiguration&lt;T&gt;</c> classes against a real Npgsql provider,
/// without ever opening a connection: accessing <see cref="DbContext.Model"/>
/// triggers full model building, including the three Aggregate Roots and their
/// owned navigations -- and, specifically for <see cref="AdministrativeDelegation"/>,
/// the JSON-converted <c>DelegatedAuthority</c> column with its custom
/// <c>ValueComparer</c>, a new EF Core shape this module introduces. This sandbox
/// has no Docker, so this is the closest verification available in this
/// environment.
/// </summary>
public sealed class AdministrationPersistenceModelTests
{
    public AdministrationPersistenceModelTests()
    {
        PersistenceAssemblyRegistry.Register(typeof(ServiceCollectionExtensions).Assembly);
    }

    [Fact]
    public void HrisDbContext_BuildsItsModel_WithoutThrowing_WhenAdministrationModuleIsRegistered()
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

        entityTypeNames.Should().Contain(typeof(UserAccount));
        entityTypeNames.Should().Contain(typeof(TenantRole));
        entityTypeNames.Should().Contain(typeof(AdministrativeDelegation));
    }

    private static HrisDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<HrisDbContext>()
            .UseNpgsql("Host=localhost;Database=model_validation_only;Username=none;Password=none")
            .Options;

        return new HrisDbContext(options);
    }
}
