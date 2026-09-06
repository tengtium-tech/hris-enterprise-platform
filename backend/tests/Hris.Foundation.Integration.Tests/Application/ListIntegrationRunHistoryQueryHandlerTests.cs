using FluentAssertions;
using Hris.Foundation.Integration.Application.Queries;
using Hris.Foundation.Integration.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.Integration.Tests.Application;

public sealed class ListIntegrationRunHistoryQueryHandlerTests
{
    private readonly IIntegrationRunRepository _repository = Substitute.For<IIntegrationRunRepository>();
    private readonly ListIntegrationRunHistoryQueryHandler _handler;

    public ListIntegrationRunHistoryQueryHandlerTests()
    {
        _handler = new ListIntegrationRunHistoryQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_ReturnsEveryRun_ForTheGivenConnector()
    {
        var connectorId = new ConnectorId(Guid.NewGuid());
        var run = TestData.StartedRun(connectorId: connectorId);
        _repository.ListHistoryAsync(TestData.TenantId, connectorId, 100, Arg.Any<CancellationToken>()).Returns([run]);

        var result = await _handler.Handle(
            new ListIntegrationRunHistoryQuery(TestData.TenantId, connectorId.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(dto => dto.IntegrationRunId == run.Id.Value);
    }
}
