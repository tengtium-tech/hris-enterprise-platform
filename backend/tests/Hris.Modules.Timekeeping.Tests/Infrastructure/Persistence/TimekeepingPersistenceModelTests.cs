using FluentAssertions;
using Hris.Infrastructure.Persistence;
using Hris.Modules.Timekeeping.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Hris.Modules.Timekeeping.Tests.Infrastructure.Persistence;

/// <summary>
/// Forces EF Core's own model validation for this module's configurations against a
/// real Npgsql provider without ever opening a connection: accessing
/// <see cref="DbContext.Model"/> triggers full model building.
///
/// Written before the Application layer on purpose. The last two Sprints both hit
/// the "no constructor parameter may bind to an owned navigation" rule, and the
/// Workflow Sprint proved the value of answering the mapping question first rather
/// than after handlers had been written against a model that might need reshaping.
/// This module has more owned surface than either: four roots, two owned entity
/// collections, three levels of owned nesting on <c>WorkShift.Timing</c>, and four
/// JSON-converted collections.
/// </summary>
public sealed class TimekeepingPersistenceModelTests
{
    public TimekeepingPersistenceModelTests()
    {
        PersistenceAssemblyRegistry.Register(typeof(ServiceCollectionExtensions).Assembly);
    }

    [Fact]
    public void HrisDbContext_BuildsItsModel_WithoutThrowing_WhenTimekeepingModuleIsRegistered()
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

        entityTypes.Should().Contain(typeof(WorkSchedule));
        entityTypes.Should().Contain(typeof(WorkShift));
        entityTypes.Should().Contain(typeof(ShiftAssignment));
        entityTypes.Should().Contain(typeof(HolidayCalendar));
    }

    [Fact]
    public void HrisDbContext_Model_MapsChildEntitiesAsOwned_NotAsAggregateRoots()
    {
        using var dbContext = CreateDbContext();

        var scheduleAssignment = dbContext.Model.GetEntityTypes().Single(t => t.ClrType == typeof(ScheduleAssignment));
        var holiday = dbContext.Model.GetEntityTypes().Single(t => t.ClrType == typeof(Holiday));

        scheduleAssignment.IsOwned().Should().BeTrue();
        holiday.IsOwned().Should().BeTrue();
    }

    private static HrisDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<HrisDbContext>()
            .UseNpgsql("Host=localhost;Database=model_validation_only;Username=none;Password=none")
            .Options;

        return new HrisDbContext(options);
    }
}
