using FluentAssertions;
using Hris.Infrastructure.Persistence;
using Hris.Modules.Workflow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Hris.Modules.Workflow.Tests;

/// <summary>
/// Confirms <see cref="ServiceCollectionExtensions.AddWorkflowModule"/> registers
/// this module's own repositories into a real <see cref="IServiceCollection"/>, the
/// same registration entry point <c>Program.cs</c> calls at real startup.
/// </summary>
public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddWorkflowModule_RegistersEveryRepository()
    {
        var services = new ServiceCollection();
        services.AddDbContext<HrisDbContext>(
            options => options.UseNpgsql("Host=localhost;Database=test;Username=none;Password=none"));

        services.AddWorkflowModule();
        var provider = services.BuildServiceProvider();

        provider.GetService<IWorkflowDefinitionRepository>().Should().NotBeNull();
        provider.GetService<IApprovalPolicyRepository>().Should().NotBeNull();
        provider.GetService<IApprovalDelegationRepository>().Should().NotBeNull();
    }

    [Fact]
    public void AddWorkflowModule_ThrowsArgumentNullException_WhenServicesIsNull()
    {
        IServiceCollection? services = null;

        var act = () => services!.AddWorkflowModule();

        act.Should().Throw<ArgumentNullException>();
    }
}
