using FluentAssertions;
using Hris.Foundation.Integration.Application.Commands;
using Hris.Foundation.Integration.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.Integration.Tests.Application;

public sealed class ActivateConnectorCommandHandlerTests
{
    private readonly IConnectorRepository _repository = Substitute.For<IConnectorRepository>();
    private readonly ActivateConnectorCommandHandler _handler;

    public ActivateConnectorCommandHandlerTests()
    {
        _handler = new ActivateConnectorCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenRegistered()
    {
        var connector = TestData.RegisteredConnector();
        _repository.GetByIdAsync(connector.Id, Arg.Any<CancellationToken>()).Returns(connector);

        var result = await _handler.Handle(new ActivateConnectorCommand(connector.Id.Value, TestData.TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        connector.Status.Should().Be(ConnectorStatus.Active);
    }

    [Fact]
    public async Task Handle_Fails_WhenConnectorDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<ConnectorId>(), Arg.Any<CancellationToken>()).Returns((Connector?)null);

        var result = await _handler.Handle(new ActivateConnectorCommand(Guid.NewGuid(), TestData.TenantId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(IntegrationErrors.ConnectorNotFound);
    }

    [Fact]
    public async Task Handle_Fails_WithConnectorNotFound_WhenConnectorBelongsToAnotherTenant()
    {
        var connector = TestData.RegisteredConnector();
        _repository.GetByIdAsync(connector.Id, Arg.Any<CancellationToken>()).Returns(connector);

        var result = await _handler.Handle(new ActivateConnectorCommand(connector.Id.Value, Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(IntegrationErrors.ConnectorNotFound);
    }
}

public sealed class SuspendConnectorCommandHandlerTests
{
    private readonly IConnectorRepository _repository = Substitute.For<IConnectorRepository>();
    private readonly SuspendConnectorCommandHandler _handler;

    public SuspendConnectorCommandHandlerTests()
    {
        _handler = new SuspendConnectorCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenActive()
    {
        var connector = TestData.ActiveConnector();
        _repository.GetByIdAsync(connector.Id, Arg.Any<CancellationToken>()).Returns(connector);

        var result = await _handler.Handle(new SuspendConnectorCommand(connector.Id.Value, TestData.TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        connector.Status.Should().Be(ConnectorStatus.Suspended);
    }

    [Fact]
    public async Task Handle_Fails_WhenConnectorDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<ConnectorId>(), Arg.Any<CancellationToken>()).Returns((Connector?)null);

        var result = await _handler.Handle(new SuspendConnectorCommand(Guid.NewGuid(), TestData.TenantId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(IntegrationErrors.ConnectorNotFound);
    }
}

public sealed class DeactivateConnectorCommandHandlerTests
{
    private readonly IConnectorRepository _repository = Substitute.For<IConnectorRepository>();
    private readonly DeactivateConnectorCommandHandler _handler;

    public DeactivateConnectorCommandHandlerTests()
    {
        _handler = new DeactivateConnectorCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenActive()
    {
        var connector = TestData.ActiveConnector();
        _repository.GetByIdAsync(connector.Id, Arg.Any<CancellationToken>()).Returns(connector);

        var result = await _handler.Handle(new DeactivateConnectorCommand(connector.Id.Value, TestData.TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        connector.Status.Should().Be(ConnectorStatus.Deactivated);
    }

    [Fact]
    public async Task Handle_Fails_WhenConnectorDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<ConnectorId>(), Arg.Any<CancellationToken>()).Returns((Connector?)null);

        var result = await _handler.Handle(new DeactivateConnectorCommand(Guid.NewGuid(), TestData.TenantId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(IntegrationErrors.ConnectorNotFound);
    }
}
