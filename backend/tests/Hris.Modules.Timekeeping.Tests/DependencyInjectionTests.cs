using FluentAssertions;
using Hris.Infrastructure.Persistence;
using Hris.Modules.Timekeeping.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Hris.Modules.Timekeeping.Tests;

/// <summary>
/// Confirms <see cref="ServiceCollectionExtensions.AddTimekeepingModule"/> registers
/// this module's four repositories into a real <see cref="IServiceCollection"/> — the
/// same entry point <c>Program.cs</c> calls at startup.
/// </summary>
public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddTimekeepingModule_RegistersEveryRepository()
    {
        var services = new ServiceCollection();
        services.AddDbContext<HrisDbContext>(
            options => options.UseNpgsql("Host=localhost;Database=test;Username=none;Password=none"));

        services.AddTimekeepingModule();
        var provider = services.BuildServiceProvider();

        provider.GetService<IWorkScheduleRepository>().Should().NotBeNull();
        provider.GetService<IWorkShiftRepository>().Should().NotBeNull();
        provider.GetService<IShiftAssignmentRepository>().Should().NotBeNull();
        provider.GetService<IHolidayCalendarRepository>().Should().NotBeNull();
    }

    [Fact]
    public void AddTimekeepingModule_ThrowsArgumentNullException_WhenServicesIsNull()
    {
        IServiceCollection? services = null;

        var act = () => services!.AddTimekeepingModule();

        act.Should().Throw<ArgumentNullException>();
    }
}
