using FluentAssertions;
using Hris.Foundation.Tenant.Application.Commands;
using Hris.Foundation.Tenant.Domain;
using NSubstitute;
using Xunit;
using TenantAggregate = Hris.Foundation.Tenant.Domain.Tenant;

namespace Hris.Foundation.Tenant.Tests.Application;

public sealed class CompleteTenantProvisioningCommandHandlerTests
{
    private readonly ITenantRepository _repository = Substitute.For<ITenantRepository>();
    private readonly CompleteTenantProvisioningCommandHandler _handler;

    public CompleteTenantProvisioningCommandHandlerTests()
    {
        _handler = new CompleteTenantProvisioningCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_AndTransitionsToConfigured_WhenTenantExists()
    {
        var tenant = TestData.RegisteredTenant();
        _repository.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);

        var result = await _handler.Handle(
            new CompleteTenantProvisioningCommand(tenant.Id.Value, Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        tenant.LifecycleState.Should().Be(TenantLifecycleState.Configured);
    }

    [Fact]
    public async Task Handle_Fails_WhenTenantDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<TenantId>(), Arg.Any<CancellationToken>()).Returns((TenantAggregate?)null);

        var result = await _handler.Handle(
            new CompleteTenantProvisioningCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TenantErrors.TenantNotFound);
    }
}
