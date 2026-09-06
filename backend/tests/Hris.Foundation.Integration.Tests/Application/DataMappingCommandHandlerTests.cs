using FluentAssertions;
using Hris.Foundation.Integration.Application.Commands;
using Hris.Foundation.Integration.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.Integration.Tests.Application;

public sealed class AddDataMappingCommandHandlerTests
{
    private readonly IConnectorRepository _repository = Substitute.For<IConnectorRepository>();
    private readonly AddDataMappingCommandHandler _handler;

    public AddDataMappingCommandHandlerTests()
    {
        _handler = new AddDataMappingCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenConnectorExistsForTheSameTenant()
    {
        var connector = TestData.RegisteredConnector();
        _repository.GetByIdAsync(connector.Id, Arg.Any<CancellationToken>()).Returns(connector);

        var result = await _handler.Handle(
            new AddDataMappingCommand(
                connector.Id.Value, TestData.TenantId, "Employee.EmployeeNumber", "ERP.PersonnelNumber", "FieldMapping", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        connector.Mappings.Should().ContainSingle(m => m.Id.Value == result.Value);
    }

    [Fact]
    public async Task Handle_Fails_WhenTransformationTypeIsNotARecognizedValue_WithoutLoadingTheConnector()
    {
        var result = await _handler.Handle(
            new AddDataMappingCommand(
                Guid.NewGuid(), TestData.TenantId, "Employee.EmployeeNumber", "ERP.PersonnelNumber", "NotARealTransformation", null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(IntegrationErrors.InvalidTransformationType);
        await _repository.DidNotReceive().GetByIdAsync(Arg.Any<ConnectorId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_WhenConnectorDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<ConnectorId>(), Arg.Any<CancellationToken>()).Returns((Connector?)null);

        var result = await _handler.Handle(
            new AddDataMappingCommand(
                Guid.NewGuid(), TestData.TenantId, "Employee.EmployeeNumber", "ERP.PersonnelNumber", "FieldMapping", null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(IntegrationErrors.ConnectorNotFound);
    }
}

public sealed class RemoveDataMappingCommandHandlerTests
{
    private readonly IConnectorRepository _repository = Substitute.For<IConnectorRepository>();
    private readonly RemoveDataMappingCommandHandler _handler;

    public RemoveDataMappingCommandHandlerTests()
    {
        _handler = new RemoveDataMappingCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenMappingExists()
    {
        var connector = TestData.RegisteredConnector();
        var mappingId = connector.AddDataMapping(
            "Employee.EmployeeNumber", "ERP.PersonnelNumber", TransformationType.FieldMapping, null, TestData.NowUtc).Value;
        _repository.GetByIdAsync(connector.Id, Arg.Any<CancellationToken>()).Returns(connector);

        var result = await _handler.Handle(
            new RemoveDataMappingCommand(connector.Id.Value, TestData.TenantId, mappingId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        connector.Mappings.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_Fails_WhenConnectorDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<ConnectorId>(), Arg.Any<CancellationToken>()).Returns((Connector?)null);

        var result = await _handler.Handle(
            new RemoveDataMappingCommand(Guid.NewGuid(), TestData.TenantId, Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(IntegrationErrors.ConnectorNotFound);
    }
}
