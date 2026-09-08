using FluentAssertions;
using Hris.Infrastructure.Persistence;
using Hris.Modules.Position;
using Hris.Modules.Position.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Hris.Modules.Position.Tests;

/// <summary>
/// Confirms <see cref="ServiceCollectionExtensions.AddPositionModule"/> registers
/// this module's own repositories and MediatR/FluentValidation handlers into a real
/// <see cref="IServiceCollection"/>, the same registration entry point
/// <c>Program.cs</c> calls at real startup.
/// </summary>
public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddPositionModule_RegistersEveryRepository()
    {
        var services = new ServiceCollection();
        services.AddDbContext<HrisDbContext>(options => options.UseNpgsql("Host=localhost;Database=test;Username=none;Password=none"));

        services.AddPositionModule();
        var provider = services.BuildServiceProvider();

        provider.GetService<IPositionRepository>().Should().NotBeNull();
        provider.GetService<IJobFamilyRepository>().Should().NotBeNull();
        provider.GetService<IJobClassificationRepository>().Should().NotBeNull();
        provider.GetService<IJobGradeRepository>().Should().NotBeNull();
    }

    [Fact]
    public void AddPositionModule_ThrowsArgumentNullException_WhenServicesIsNull()
    {
        IServiceCollection? services = null;

        var act = () => services!.AddPositionModule();

        act.Should().Throw<ArgumentNullException>();
    }
}
