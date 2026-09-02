using FluentAssertions;
using Hris.Foundation.Tenant.Application.Queries;
using Hris.Foundation.Tenant.Domain;
using NSubstitute;
using Xunit;
using TenantAggregate = Hris.Foundation.Tenant.Domain.Tenant;

namespace Hris.Foundation.Tenant.Tests.Application;

public sealed class ListTenantsQueryHandlerTests
{
    private readonly ITenantRepository _repository = Substitute.For<ITenantRepository>();
    private readonly ListTenantsQueryHandler _handler;

    public ListTenantsQueryHandlerTests()
    {
        _handler = new ListTenantsQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_ReturnsEveryTenantsRegistryRecord()
    {
        IReadOnlyCollection<TenantAggregate> tenants = [TestData.RegisteredTenant(), TestData.ActiveTenant()];
        _repository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(tenants);

        var result = await _handler.Handle(new ListTenantsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_ReturnsAnEmptyCollection_WhenNoTenantsExist()
    {
        _repository.GetAllAsync(Arg.Any<CancellationToken>()).Returns((IReadOnlyCollection<TenantAggregate>)[]);

        var result = await _handler.Handle(new ListTenantsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
