using FluentAssertions;
using Hris.Foundation.Integration.Application.Queries;
using Hris.Foundation.Integration.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.Integration.Tests.Application;

public sealed class ListConnectorsQueryHandlerTests
{
    private readonly IConnectorRepository _repository = Substitute.For<IConnectorRepository>();
    private readonly ListConnectorsQueryHandler _handler;

    public ListConnectorsQueryHandlerTests()
    {
        _handler = new ListConnectorsQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_ReturnsEveryConnector_ForTheGivenTenant()
    {
        var connector = TestData.RegisteredConnector();
        _repository.ListByTenantAsync(TestData.TenantId, Arg.Any<CancellationToken>()).Returns([connector]);

        var result = await _handler.Handle(new ListConnectorsQuery(TestData.TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var dto = result.Value.Should().ContainSingle(dto => dto.ConnectorId == connector.Id.Value).Subject;
        dto.Name.Should().Be(connector.Name);
        dto.IntegrationCategory.Should().Be(connector.IntegrationCategory);
        dto.EndpointType.Should().Be(connector.EndpointType.ToString());
        dto.Status.Should().Be(connector.Status.ToString());
    }
}
