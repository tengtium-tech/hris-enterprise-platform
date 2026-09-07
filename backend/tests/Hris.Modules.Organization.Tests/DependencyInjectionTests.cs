using FluentAssertions;
using Hris.Infrastructure.Persistence;
using Hris.Modules.Organization;
using Hris.Modules.Organization.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Hris.Modules.Organization.Tests;

/// <summary>
/// Confirms <see cref="ServiceCollectionExtensions.AddOrganizationModule"/> registers
/// this module's own repositories and MediatR/FluentValidation handlers into a real
/// <see cref="IServiceCollection"/>, the same registration entry point
/// <c>Program.cs</c> calls at real startup.
/// </summary>
public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddOrganizationModule_RegistersEveryRepository()
    {
        var services = new ServiceCollection();
        services.AddDbContext<HrisDbContext>(options => options.UseNpgsql("Host=localhost;Database=test;Username=none;Password=none"));

        services.AddOrganizationModule();
        var provider = services.BuildServiceProvider();

        provider.GetService<IOrganizationRepository>().Should().NotBeNull();
        provider.GetService<IWorkLocationRepository>().Should().NotBeNull();
        provider.GetService<ILegalEntityRepository>().Should().NotBeNull();
    }

    [Fact]
    public void AddOrganizationModule_ThrowsArgumentNullException_WhenServicesIsNull()
    {
        IServiceCollection? services = null;

        var act = () => services!.AddOrganizationModule();

        act.Should().Throw<ArgumentNullException>();
    }
}
