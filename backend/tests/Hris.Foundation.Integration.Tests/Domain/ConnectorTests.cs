using FluentAssertions;
using Hris.Foundation.Integration.Domain;
using System.Linq;
using Xunit;

namespace Hris.Foundation.Integration.Tests.Domain;

public sealed class ConnectorTests
{
    [Fact]
    public void Register_Succeeds_AndStartsInRegistered()
    {
        var result = Connector.Register(
            TestData.TenantId, "SAP Connector", "ERP", EndpointType.RestApi, "https://sap.example.com/api", TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(ConnectorStatus.Registered);
        var raised = result.Value.DomainEvents.OfType<ConnectorRegistered>().Should().ContainSingle().Subject;
        raised.ConnectorId.Should().Be(result.Value.Id);
        raised.TenantId.Should().Be(TestData.TenantId);
        raised.IntegrationCategory.Should().Be("ERP");
    }

    [Fact]
    public void Register_Fails_WhenNameIsEmpty()
    {
        var result = Connector.Register(
            TestData.TenantId, string.Empty, "ERP", EndpointType.RestApi, "https://sap.example.com/api", TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(IntegrationErrors.NameRequired);
    }

    [Fact]
    public void Register_Fails_WhenIntegrationCategoryIsEmpty()
    {
        var result = Connector.Register(
            TestData.TenantId, "SAP Connector", string.Empty, EndpointType.RestApi, "https://sap.example.com/api", TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(IntegrationErrors.IntegrationCategoryRequired);
    }

    [Fact]
    public void Register_Fails_WhenEndpointAddressIsEmpty()
    {
        var result = Connector.Register(TestData.TenantId, "SAP Connector", "ERP", EndpointType.RestApi, string.Empty, TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(IntegrationErrors.EndpointAddressRequired);
    }

    [Fact]
    public void Register_Fails_WhenTenantIdIsEmpty()
    {
        var act = () => Connector.Register(
            Guid.Empty, "SAP Connector", "ERP", EndpointType.RestApi, "https://sap.example.com/api", TestData.NowUtc);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Activate_Succeeds_FromRegistered()
    {
        var connector = TestData.RegisteredConnector();
        connector.ClearDomainEvents();

        var result = connector.Activate(TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        connector.Status.Should().Be(ConnectorStatus.Active);
        connector.DomainEvents.Should().ContainSingle(e => e is ConnectorUpdated);
    }

    [Fact]
    public void Activate_Succeeds_FromSuspended()
    {
        var connector = TestData.SuspendedConnector();

        var result = connector.Activate(TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        connector.Status.Should().Be(ConnectorStatus.Active);
    }

    [Fact]
    public void Activate_Fails_WhenDeactivated()
    {
        var connector = TestData.DeactivatedConnector();

        var result = connector.Activate(TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(IntegrationErrors.InvalidConnectorLifecycleTransition);
    }

    [Fact]
    public void Suspend_Succeeds_FromActive()
    {
        var connector = TestData.ActiveConnector();

        var result = connector.Suspend(TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        connector.Status.Should().Be(ConnectorStatus.Suspended);
    }

    [Fact]
    public void Suspend_Fails_WhenNotActive()
    {
        var connector = TestData.RegisteredConnector();

        var result = connector.Suspend(TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(IntegrationErrors.InvalidConnectorLifecycleTransition);
    }

    [Fact]
    public void Deactivate_Succeeds_FromActive()
    {
        var connector = TestData.ActiveConnector();

        var result = connector.Deactivate(TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        connector.Status.Should().Be(ConnectorStatus.Deactivated);
    }

    [Fact]
    public void Deactivate_Fails_WhenAlreadyDeactivated()
    {
        var connector = TestData.DeactivatedConnector();

        var result = connector.Deactivate(TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(IntegrationErrors.InvalidConnectorLifecycleTransition);
    }

    [Fact]
    public void UpdateEndpoint_Succeeds_AndReplacesTypeAndAddress()
    {
        var connector = TestData.RegisteredConnector();

        var result = connector.UpdateEndpoint(EndpointType.Sftp, "sftp://sap.example.com/inbound", TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        connector.EndpointType.Should().Be(EndpointType.Sftp);
        connector.EndpointAddress.Should().Be("sftp://sap.example.com/inbound");
    }

    [Fact]
    public void UpdateEndpoint_Fails_WhenDeactivated()
    {
        var connector = TestData.DeactivatedConnector();

        var result = connector.UpdateEndpoint(EndpointType.Sftp, "sftp://sap.example.com/inbound", TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(IntegrationErrors.InvalidConnectorLifecycleTransition);
    }

    [Fact]
    public void UpdateEndpoint_Fails_WhenAddressIsEmpty()
    {
        var connector = TestData.RegisteredConnector();

        var result = connector.UpdateEndpoint(EndpointType.Sftp, string.Empty, TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(IntegrationErrors.EndpointAddressRequired);
    }

    [Fact]
    public void AddDataMapping_Succeeds_AndAppendsToMappings()
    {
        var connector = TestData.RegisteredConnector();

        var result = connector.AddDataMapping(
            "Employee.EmployeeNumber", "ERP.PersonnelNumber", TransformationType.FieldMapping, null, TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        connector.Mappings.Should().ContainSingle(m => m.Id.Value == result.Value);
        connector.Mappings[0].SourceField.Should().Be("Employee.EmployeeNumber");
        connector.Mappings[0].TargetField.Should().Be("ERP.PersonnelNumber");
    }

    [Fact]
    public void AddDataMapping_Fails_WhenSourceFieldIsEmpty()
    {
        var connector = TestData.RegisteredConnector();

        var result = connector.AddDataMapping(string.Empty, "ERP.PersonnelNumber", TransformationType.FieldMapping, null, TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(IntegrationErrors.SourceFieldRequired);
    }

    [Fact]
    public void AddDataMapping_Fails_WhenTargetFieldIsEmpty()
    {
        var connector = TestData.RegisteredConnector();

        var result = connector.AddDataMapping(
            "Employee.EmployeeNumber", string.Empty, TransformationType.FieldMapping, null, TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(IntegrationErrors.TargetFieldRequired);
    }

    [Fact]
    public void AddDataMapping_Fails_WhenDeactivated()
    {
        var connector = TestData.DeactivatedConnector();

        var result = connector.AddDataMapping(
            "Employee.EmployeeNumber", "ERP.PersonnelNumber", TransformationType.FieldMapping, null, TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(IntegrationErrors.InvalidConnectorLifecycleTransition);
    }

    [Fact]
    public void RemoveDataMapping_Succeeds_AndRemovesFromMappings()
    {
        var connector = TestData.RegisteredConnector();
        var mappingId = connector.AddDataMapping(
            "Employee.EmployeeNumber", "ERP.PersonnelNumber", TransformationType.FieldMapping, null, TestData.NowUtc).Value;

        var result = connector.RemoveDataMapping(mappingId, TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        connector.Mappings.Should().BeEmpty();
    }

    [Fact]
    public void RemoveDataMapping_Fails_WhenMappingDoesNotExist()
    {
        var connector = TestData.RegisteredConnector();

        var result = connector.RemoveDataMapping(Guid.NewGuid(), TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(IntegrationErrors.DataMappingNotFound);
    }
}
