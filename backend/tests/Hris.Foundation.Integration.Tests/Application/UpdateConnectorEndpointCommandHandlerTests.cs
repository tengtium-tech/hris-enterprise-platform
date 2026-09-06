using FluentAssertions;
using Hris.Foundation.Integration.Application.Commands;
using Hris.Foundation.Integration.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.Integration.Tests.Application;

public sealed class UpdateConnectorEndpointCommandHandlerTests
{
    private readonly IConnectorRepository _repository = Substitute.For<IConnectorRepository>();
    private readonly UpdateConnectorEndpointCommandHandler _handler;

    public UpdateConnectorEndpointCommandHandlerTests()
    {
        _handler = new UpdateConnectorEndpointCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenConnectorExistsForTheSameTenant()
    {
        var connector = TestData.RegisteredConnector();
        _repository.GetByIdAsync(connector.Id, Arg.Any<CancellationToken>()).Returns(connector);

        var result = await _handler.Handle(
            new UpdateConnectorEndpointCommand(connector.Id.Value, TestData.TenantId, "Sftp", "sftp://sap.example.com/inbound"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        connector.EndpointType.Should().Be(EndpointType.Sftp);
    }

    [Fact]
    public async Task Handle_Fails_WhenEndpointTypeIsNotARecognizedValue_WithoutLoadingTheConnector()
    {
        var result = await _handler.Handle(
            new UpdateConnectorEndpointCommand(Guid.NewGuid(), TestData.TenantId, "NotARealEndpointType", "sftp://sap.example.com"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(IntegrationErrors.InvalidEndpointType);
        await _repository.DidNotReceive().GetByIdAsync(Arg.Any<ConnectorId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_WhenConnectorDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<ConnectorId>(), Arg.Any<CancellationToken>()).Returns((Connector?)null);

        var result = await _handler.Handle(
            new UpdateConnectorEndpointCommand(Guid.NewGuid(), TestData.TenantId, "Sftp", "sftp://sap.example.com"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(IntegrationErrors.ConnectorNotFound);
    }
}
