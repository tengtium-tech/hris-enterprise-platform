using FluentAssertions;
using Hris.Foundation.Integration.Application.Commands;
using Hris.Foundation.Integration.Application.Queries;
using Hris.Foundation.Integration.Application.Validators;
using Xunit;

namespace Hris.Foundation.Integration.Tests.Application;

/// <summary>
/// One valid-passes/invalid-fails pair per validator, the identical shape
/// <c>FileStorageCommandValidatorsTests</c>/<c>DocumentManagementCommandValidatorsTests</c>
/// already establish.
/// </summary>
public sealed class IntegrationCommandValidatorsTests
{
    [Fact]
    public void RegisterConnectorCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyName()
    {
        var validator = new RegisterConnectorCommandValidator();
        var valid = new RegisterConnectorCommand(Guid.NewGuid(), "SAP Connector", "ERP", "RestApi", "https://sap.example.com");
        var invalid = valid with { Name = string.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ActivateConnectorCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyConnectorId()
    {
        var validator = new ActivateConnectorCommandValidator();

        validator.Validate(new ActivateConnectorCommand(Guid.NewGuid(), Guid.NewGuid())).IsValid.Should().BeTrue();
        validator.Validate(new ActivateConnectorCommand(Guid.Empty, Guid.NewGuid())).IsValid.Should().BeFalse();
    }

    [Fact]
    public void SuspendConnectorCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyConnectorId()
    {
        var validator = new SuspendConnectorCommandValidator();

        validator.Validate(new SuspendConnectorCommand(Guid.NewGuid(), Guid.NewGuid())).IsValid.Should().BeTrue();
        validator.Validate(new SuspendConnectorCommand(Guid.Empty, Guid.NewGuid())).IsValid.Should().BeFalse();
    }

    [Fact]
    public void DeactivateConnectorCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyConnectorId()
    {
        var validator = new DeactivateConnectorCommandValidator();

        validator.Validate(new DeactivateConnectorCommand(Guid.NewGuid(), Guid.NewGuid())).IsValid.Should().BeTrue();
        validator.Validate(new DeactivateConnectorCommand(Guid.Empty, Guid.NewGuid())).IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdateConnectorEndpointCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyEndpointAddress()
    {
        var validator = new UpdateConnectorEndpointCommandValidator();
        var valid = new UpdateConnectorEndpointCommand(Guid.NewGuid(), Guid.NewGuid(), "RestApi", "https://sap.example.com");
        var invalid = valid with { EndpointAddress = string.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void AddDataMappingCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptySourceField()
    {
        var validator = new AddDataMappingCommandValidator();
        var valid = new AddDataMappingCommand(
            Guid.NewGuid(), Guid.NewGuid(), "Employee.EmployeeNumber", "ERP.PersonnelNumber", "FieldMapping", null);
        var invalid = valid with { SourceField = string.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void RemoveDataMappingCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyDataMappingId()
    {
        var validator = new RemoveDataMappingCommandValidator();
        var valid = new RemoveDataMappingCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var invalid = valid with { DataMappingId = Guid.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void StartIntegrationRunCommandValidator_AcceptsAValidCommand_AndRejectsANegativeMaxRetries()
    {
        var validator = new StartIntegrationRunCommandValidator();
        var valid = new StartIntegrationRunCommand(Guid.NewGuid(), Guid.NewGuid(), "IntegrationCall", null, null, 3);
        var invalid = valid with { MaxRetries = -1 };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CompleteIntegrationRunCommandValidator_AcceptsAValidCommand_AndRejectsANegativeRecordsProcessed()
    {
        var validator = new CompleteIntegrationRunCommandValidator();
        var valid = new CompleteIntegrationRunCommand(Guid.NewGuid(), Guid.NewGuid(), 10);
        var invalid = valid with { RecordsProcessed = -1 };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void FailIntegrationRunCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyReason()
    {
        var validator = new FailIntegrationRunCommandValidator();
        var valid = new FailIntegrationRunCommand(Guid.NewGuid(), Guid.NewGuid(), "timeout");
        var invalid = valid with { Reason = string.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void RetryIntegrationRunCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyRunId()
    {
        var validator = new RetryIntegrationRunCommandValidator();

        validator.Validate(new RetryIntegrationRunCommand(Guid.NewGuid(), Guid.NewGuid())).IsValid.Should().BeTrue();
        validator.Validate(new RetryIntegrationRunCommand(Guid.Empty, Guid.NewGuid())).IsValid.Should().BeFalse();
    }

    [Fact]
    public void MoveIntegrationRunToDeadLetterCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyRunId()
    {
        var validator = new MoveIntegrationRunToDeadLetterCommandValidator();

        validator.Validate(new MoveIntegrationRunToDeadLetterCommand(Guid.NewGuid(), Guid.NewGuid())).IsValid.Should().BeTrue();
        validator.Validate(new MoveIntegrationRunToDeadLetterCommand(Guid.Empty, Guid.NewGuid())).IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetConnectorQueryValidator_AcceptsAValidQuery_AndRejectsAnEmptyConnectorId()
    {
        var validator = new GetConnectorQueryValidator();

        validator.Validate(new GetConnectorQuery(Guid.NewGuid(), Guid.NewGuid())).IsValid.Should().BeTrue();
        validator.Validate(new GetConnectorQuery(Guid.Empty, Guid.NewGuid())).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ListConnectorsQueryValidator_AcceptsAValidQuery_AndRejectsAnEmptyTenantId()
    {
        var validator = new ListConnectorsQueryValidator();

        validator.Validate(new ListConnectorsQuery(Guid.NewGuid())).IsValid.Should().BeTrue();
        validator.Validate(new ListConnectorsQuery(Guid.Empty)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetIntegrationRunQueryValidator_AcceptsAValidQuery_AndRejectsAnEmptyRunId()
    {
        var validator = new GetIntegrationRunQueryValidator();

        validator.Validate(new GetIntegrationRunQuery(Guid.NewGuid(), Guid.NewGuid())).IsValid.Should().BeTrue();
        validator.Validate(new GetIntegrationRunQuery(Guid.Empty, Guid.NewGuid())).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ListIntegrationRunHistoryQueryValidator_AcceptsAValidQuery_AndRejectsAnEmptyConnectorId()
    {
        var validator = new ListIntegrationRunHistoryQueryValidator();
        var valid = new ListIntegrationRunHistoryQuery(Guid.NewGuid(), Guid.NewGuid());
        var invalid = valid with { ConnectorId = Guid.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }
}
