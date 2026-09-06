using FluentAssertions;
using Hris.Foundation.Integration.Application.Commands;
using Hris.Foundation.Integration.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.Integration.Tests.Application;

public sealed class RegisterConnectorCommandHandlerTests
{
    private readonly IConnectorRepository _repository = Substitute.For<IConnectorRepository>();
    private readonly RegisterConnectorCommandHandler _handler;

    public RegisterConnectorCommandHandlerTests()
    {
        _handler = new RegisterConnectorCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_AndPersistsTheNewConnector_WhenInputIsValid()
    {
        var command = new RegisterConnectorCommand(TestData.TenantId, "SAP Connector", "ERP", "RestApi", "https://sap.example.com/api");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).AddAsync(Arg.Any<Connector>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_WhenEndpointTypeIsNotARecognizedValue_WithoutCallingTheRepository()
    {
        var command = new RegisterConnectorCommand(
            TestData.TenantId, "SAP Connector", "ERP", "NotARealEndpointType", "https://sap.example.com/api");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(IntegrationErrors.InvalidEndpointType);
        await _repository.DidNotReceive().AddAsync(Arg.Any<Connector>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_WhenNameIsEmpty_WithoutCallingTheRepository()
    {
        var command = new RegisterConnectorCommand(TestData.TenantId, string.Empty, "ERP", "RestApi", "https://sap.example.com/api");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(IntegrationErrors.NameRequired);
        await _repository.DidNotReceive().AddAsync(Arg.Any<Connector>(), Arg.Any<CancellationToken>());
    }
}
