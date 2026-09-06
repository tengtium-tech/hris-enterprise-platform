using FluentAssertions;
using Hris.Foundation.Integration.Application.Commands;
using Hris.Foundation.Integration.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.Integration.Tests.Application;

public sealed class StartIntegrationRunCommandHandlerTests
{
    private readonly IConnectorRepository _connectorRepository = Substitute.For<IConnectorRepository>();
    private readonly IIntegrationRunRepository _runRepository = Substitute.For<IIntegrationRunRepository>();
    private readonly StartIntegrationRunCommandHandler _handler;

    public StartIntegrationRunCommandHandlerTests()
    {
        _handler = new StartIntegrationRunCommandHandler(_connectorRepository, _runRepository, new FakeTimeProvider(TestData.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenConnectorExistsForTheSameTenant()
    {
        var connector = TestData.ActiveConnector();
        _connectorRepository.GetByIdAsync(connector.Id, Arg.Any<CancellationToken>()).Returns(connector);

        var result = await _handler.Handle(
            new StartIntegrationRunCommand(TestData.TenantId, connector.Id.Value, "IntegrationCall", null, null, MaxRetries: 3),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _runRepository.Received(1).AddAsync(Arg.Any<IntegrationRun>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_WhenRunKindIsNotARecognizedValue_WithoutLoadingTheConnector()
    {
        var result = await _handler.Handle(
            new StartIntegrationRunCommand(TestData.TenantId, Guid.NewGuid(), "NotARealRunKind", null, null, MaxRetries: 3),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(IntegrationErrors.InvalidRunKind);
        await _connectorRepository.DidNotReceive().GetByIdAsync(Arg.Any<ConnectorId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_WhenConnectorDoesNotExist_WithoutCallingTheRunRepository()
    {
        _connectorRepository.GetByIdAsync(Arg.Any<ConnectorId>(), Arg.Any<CancellationToken>()).Returns((Connector?)null);

        var result = await _handler.Handle(
            new StartIntegrationRunCommand(TestData.TenantId, Guid.NewGuid(), "IntegrationCall", null, null, MaxRetries: 3),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(IntegrationErrors.ConnectorNotFound);
        await _runRepository.DidNotReceive().AddAsync(Arg.Any<IntegrationRun>(), Arg.Any<CancellationToken>());
    }
}
