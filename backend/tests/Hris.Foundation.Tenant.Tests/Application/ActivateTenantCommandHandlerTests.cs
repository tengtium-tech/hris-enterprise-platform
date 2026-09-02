using FluentAssertions;
using Hris.Foundation.Tenant.Application.Commands;
using Hris.Foundation.Tenant.Domain;
using NSubstitute;
using Xunit;
using TenantAggregate = Hris.Foundation.Tenant.Domain.Tenant;

namespace Hris.Foundation.Tenant.Tests.Application;

public sealed class ActivateTenantCommandHandlerTests
{
    private readonly ITenantRepository _repository = Substitute.For<ITenantRepository>();
    private readonly ActivateTenantCommandHandler _handler;

    public ActivateTenantCommandHandlerTests()
    {
        _handler = new ActivateTenantCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_AndTransitionsToActive_WhenTenantExists()
    {
        var tenant = TestData.ConfiguredTenant();
        _repository.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);

        var result = await _handler.Handle(new ActivateTenantCommand(tenant.Id.Value, Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        tenant.LifecycleState.Should().Be(TenantLifecycleState.Active);
    }

    [Fact]
    public async Task Handle_Fails_WhenTenantDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<TenantId>(), Arg.Any<CancellationToken>()).Returns((TenantAggregate?)null);

        var result = await _handler.Handle(new ActivateTenantCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TenantErrors.TenantNotFound);
    }
}
