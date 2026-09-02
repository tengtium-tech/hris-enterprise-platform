using FluentAssertions;
using Hris.Foundation.Tenant.Application.Queries;
using Hris.Foundation.Tenant.Domain;
using NSubstitute;
using Xunit;
using TenantAggregate = Hris.Foundation.Tenant.Domain.Tenant;

namespace Hris.Foundation.Tenant.Tests.Application;

public sealed class GetTenantQueryHandlerTests
{
    private readonly ITenantRepository _repository = Substitute.For<ITenantRepository>();
    private readonly GetTenantQueryHandler _handler;

    public GetTenantQueryHandlerTests()
    {
        _handler = new GetTenantQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_ReturnsTheTenantsRegistryRecord_WhenItExists()
    {
        var tenant = TestData.ActiveTenant();
        _repository.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);

        var result = await _handler.Handle(new GetTenantQuery(tenant.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TenantId.Should().Be(tenant.Id.Value);
        result.Value.TenantCode.Should().Be(tenant.TenantCode.Value);
    }

    [Fact]
    public async Task Handle_Fails_WhenTenantDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<TenantId>(), Arg.Any<CancellationToken>()).Returns((TenantAggregate?)null);

        var result = await _handler.Handle(new GetTenantQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TenantErrors.TenantNotFound);
    }
}
