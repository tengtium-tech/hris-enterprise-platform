using FluentAssertions;
using Hris.Foundation.Tenant.Application.Commands;
using Hris.Foundation.Tenant.Domain;
using NSubstitute;
using Xunit;
using TenantAggregate = Hris.Foundation.Tenant.Domain.Tenant;

namespace Hris.Foundation.Tenant.Tests.Application;

public sealed class ChangeTenantSubscriptionPlanCommandHandlerTests
{
    private readonly ITenantRepository _repository = Substitute.For<ITenantRepository>();
    private readonly ChangeTenantSubscriptionPlanCommandHandler _handler;

    public ChangeTenantSubscriptionPlanCommandHandlerTests()
    {
        _handler = new ChangeTenantSubscriptionPlanCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenTenantIsActive()
    {
        var tenant = TestData.ActiveTenant();
        _repository.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);

        var result = await _handler.Handle(
            new ChangeTenantSubscriptionPlanCommand(tenant.Id.Value, SubscriptionPlan.Enterprise, Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        tenant.SubscriptionPlan.Should().Be(SubscriptionPlan.Enterprise);
    }

    [Fact]
    public async Task Handle_Fails_WhenTenantIsNotActive()
    {
        var tenant = TestData.ConfiguredTenant();
        _repository.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);

        var result = await _handler.Handle(
            new ChangeTenantSubscriptionPlanCommand(tenant.Id.Value, SubscriptionPlan.Enterprise, Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TenantErrors.SubscriptionPlanChangeRequiresActive);
    }

    [Fact]
    public async Task Handle_Fails_WhenTenantDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<TenantId>(), Arg.Any<CancellationToken>()).Returns((TenantAggregate?)null);

        var result = await _handler.Handle(
            new ChangeTenantSubscriptionPlanCommand(Guid.NewGuid(), SubscriptionPlan.Enterprise, Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TenantErrors.TenantNotFound);
    }
}
