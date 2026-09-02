using FluentAssertions;
using Hris.Foundation.Tenant.Application.Commands;
using Hris.Foundation.Tenant.Domain;
using NSubstitute;
using Xunit;
using TenantAggregate = Hris.Foundation.Tenant.Domain.Tenant;

namespace Hris.Foundation.Tenant.Tests.Application;

public sealed class RegisterTenantCommandHandlerTests
{
    private readonly ITenantRepository _repository = Substitute.For<ITenantRepository>();
    private readonly RegisterTenantCommandHandler _handler;

    public RegisterTenantCommandHandlerTests()
    {
        _handler = new RegisterTenantCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));
    }

    private static RegisterTenantCommand ValidCommand(string tenantCode = "acme-corp") => new(
        tenantCode,
        "ACME Manufacturing",
        SubscriptionPlan.Growth,
        "en-PH",
        "PHP",
        "Asia/Manila",
        "Jane Administrator",
        "jane@acme.example",
        TestData.NewPlatformOperatorId());

    [Fact]
    public async Task Handle_Succeeds_AndPersistsTheNewTenant_WhenTenantCodeIsAvailable()
    {
        _repository.ExistsByTenantCodeAsync(Arg.Any<TenantCode>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).AddAsync(Arg.Any<TenantAggregate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_WhenTenantCodeIsAlreadyRegistered()
    {
        _repository.ExistsByTenantCodeAsync(Arg.Any<TenantCode>(), Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TenantErrors.TenantCodeAlreadyRegistered);
        await _repository.DidNotReceive().AddAsync(Arg.Any<TenantAggregate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_WhenTenantCodeIsInvalid_WithoutCallingTheRepository()
    {
        var result = await _handler.Handle(ValidCommand(tenantCode: "a"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TenantErrors.TenantCodeInvalidFormat);
        await _repository.DidNotReceive().ExistsByTenantCodeAsync(Arg.Any<TenantCode>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_WhenOrganizationIsEmpty()
    {
        _repository.ExistsByTenantCodeAsync(Arg.Any<TenantCode>(), Arg.Any<CancellationToken>()).Returns(false);

        var command = ValidCommand() with { Organization = string.Empty };
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TenantErrors.OrganizationRequired);
    }
}
