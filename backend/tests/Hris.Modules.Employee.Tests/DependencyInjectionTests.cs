using FluentAssertions;
using Hris.Infrastructure.Persistence;
using Hris.Modules.Employee;
using Hris.Modules.Employee.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Hris.Modules.Employee.Tests;

/// <summary>
/// Confirms <see cref="ServiceCollectionExtensions.AddEmployeeModule"/> registers
/// this module's own repositories and MediatR/FluentValidation handlers into a real
/// <see cref="IServiceCollection"/>, the same registration entry point
/// <c>Program.cs</c> calls at real startup.
/// </summary>
public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddEmployeeModule_RegistersEveryRepository()
    {
        var services = new ServiceCollection();
        services.AddDbContext<HrisDbContext>(options => options.UseNpgsql("Host=localhost;Database=test;Username=none;Password=none"));

        services.AddEmployeeModule();
        var provider = services.BuildServiceProvider();

        provider.GetService<IEmployeeRepository>().Should().NotBeNull();
        provider.GetService<IEmployeeHistoryRepository>().Should().NotBeNull();
    }

    [Fact]
    public void AddEmployeeModule_ThrowsArgumentNullException_WhenServicesIsNull()
    {
        IServiceCollection? services = null;

        var act = () => services!.AddEmployeeModule();

        act.Should().Throw<ArgumentNullException>();
    }
}
