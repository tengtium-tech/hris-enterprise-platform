using FluentAssertions;
using Hris.Foundation.Tenant.Application.Commands;
using Hris.Foundation.Tenant.Domain;
using NSubstitute;
using Xunit;
using TenantAggregate = Hris.Foundation.Tenant.Domain.Tenant;

namespace Hris.Foundation.Tenant.Tests.Application;

public sealed class UpdateTenantOrganizationNameCommandHandlerTests
{
    private readonly ITenantRepository _repository = Substitute.For<ITenantRepository>();
    private readonly UpdateTenantOrganizationNameCommandHandler _handler;

    public UpdateTenantOrganizationNameCommandHandlerTests()
    {
        _handler = new UpdateTenantOrganizationNameCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenTenantExists()
    {
        var tenant = TestData.ActiveTenant();
        _repository.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);

        var result = await _handler.Handle(
            new UpdateTenantOrganizationNameCommand(tenant.Id.Value, "New Name Inc.", Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        tenant.Organization.Should().Be("New Name Inc.");
    }

    [Fact]
    public async Task Handle_Fails_WhenTenantIsDeleted()
    {
        var tenant = TestData.DeletedTenant();
        _repository.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);

        var result = await _handler.Handle(
            new UpdateTenantOrganizationNameCommand(tenant.Id.Value, "New Name Inc.", Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TenantErrors.OrganizationNameUpdateRejectedWhenDeleted);
    }

    [Fact]
    public async Task Handle_Fails_WhenTenantDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<TenantId>(), Arg.Any<CancellationToken>()).Returns((TenantAggregate?)null);

        var result = await _handler.Handle(
            new UpdateTenantOrganizationNameCommand(Guid.NewGuid(), "New Name Inc.", Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TenantErrors.TenantNotFound);
    }
}
