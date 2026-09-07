using FluentAssertions;
using Hris.Infrastructure.Persistence;
using Hris.Modules.Organization.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Hris.Modules.Organization.Tests.Infrastructure.Persistence;

/// <summary>
/// Forces EF Core's own model validation for this module's own
/// <c>IEntityTypeConfiguration&lt;T&gt;</c> classes against a real Npgsql provider,
/// without ever opening a connection: accessing <see cref="DbContext.Model"/>
/// triggers full model building, including the five-level nested
/// <c>OwnsMany</c>-within-<c>OwnsMany</c> chain
/// (Organization -&gt; BusinessUnit -&gt; Division -&gt; Department -&gt; Section -&gt;
/// Team). This sandbox has no Docker, so <c>Hris.Api.Tests</c>' own Testcontainers-
/// backed host (which would otherwise exercise this same model against a real
/// database) cannot run here -- this is the closest verification available in this
/// environment, and it is exactly the shape of check most likely to catch a real
/// modeling mistake in a chain this deep (a wrong <c>HasForeignKey</c> target, a
/// missing owned-type callback, and so on all throw at model-build time, not at
/// query time).
/// </summary>
public sealed class OrganizationPersistenceModelTests
{
    public OrganizationPersistenceModelTests()
    {
        PersistenceAssemblyRegistry.Register(typeof(ServiceCollectionExtensions).Assembly);
    }

    [Fact]
    public void HrisDbContext_BuildsItsModel_WithoutThrowing_WhenOrganizationModuleIsRegistered()
    {
        using var dbContext = CreateDbContext();

        var act = () => dbContext.Model;

        act.Should().NotThrow();
    }

    [Fact]
    public void HrisDbContext_Model_IncludesEveryLevelOfTheOrganizationHierarchy()
    {
        using var dbContext = CreateDbContext();

        var entityTypeNames = dbContext.Model.GetEntityTypes().Select(t => t.ClrType).ToList();

        entityTypeNames.Should().Contain(typeof(Hris.Modules.Organization.Domain.Organization));
        entityTypeNames.Should().Contain(typeof(BusinessUnit));
        entityTypeNames.Should().Contain(typeof(Division));
        entityTypeNames.Should().Contain(typeof(Department));
        entityTypeNames.Should().Contain(typeof(Section));
        entityTypeNames.Should().Contain(typeof(Team));
        entityTypeNames.Should().Contain(typeof(CostCenter));
        entityTypeNames.Should().Contain(typeof(WorkLocation));
        entityTypeNames.Should().Contain(typeof(LegalEntity));
    }

    private static HrisDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<HrisDbContext>()
            .UseNpgsql("Host=localhost;Database=model_validation_only;Username=none;Password=none")
            .Options;

        return new HrisDbContext(options);
    }
}
