using FluentAssertions;
using Hris.Foundation.Integration.Application.Queries;
using Hris.Foundation.Integration.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.Integration.Tests.Application;

public sealed class GetConnectorQueryHandlerTests
{
    private readonly IConnectorRepository _repository = Substitute.For<IConnectorRepository>();
    private readonly GetConnectorQueryHandler _handler;

    public GetConnectorQueryHandlerTests()
    {
        _handler = new GetConnectorQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_Succeeds_WhenConnectorExistsForTheSameTenant()
    {
        var connector = TestData.RegisteredConnector();
        var mappingId = connector.AddDataMapping(
            "Employee.EmployeeNumber", "ERP.PersonnelNumber", TransformationType.FieldMapping, "uppercase", TestData.NowUtc).Value;
        _repository.GetByIdAsync(connector.Id, Arg.Any<CancellationToken>()).Returns(connector);

        var result = await _handler.Handle(new GetConnectorQuery(connector.Id.Value, TestData.TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var dto = result.Value;
        dto.ConnectorId.Should().Be(connector.Id.Value);
        dto.TenantId.Should().Be(connector.TenantId);
        dto.Name.Should().Be(connector.Name);
        dto.IntegrationCategory.Should().Be(connector.IntegrationCategory);
        dto.EndpointType.Should().Be(connector.EndpointType.ToString());
        dto.EndpointAddress.Should().Be(connector.EndpointAddress);
        dto.Status.Should().Be(connector.Status.ToString());
        dto.CreatedAtUtc.Should().Be(connector.CreatedAtUtc);
        var mappingDto = dto.Mappings.Should().ContainSingle().Subject;
        mappingDto.DataMappingId.Should().Be(mappingId);
        mappingDto.SourceField.Should().Be("Employee.EmployeeNumber");
        mappingDto.TargetField.Should().Be("ERP.PersonnelNumber");
        mappingDto.TransformationType.Should().Be("FieldMapping");
        mappingDto.TransformationRule.Should().Be("uppercase");
    }

    [Fact]
    public async Task Handle_Fails_WhenConnectorDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<ConnectorId>(), Arg.Any<CancellationToken>()).Returns((Connector?)null);

        var result = await _handler.Handle(new GetConnectorQuery(Guid.NewGuid(), TestData.TenantId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(IntegrationErrors.ConnectorNotFound);
    }

    /// <summary>
    /// This document's own AI Implementation Guidance names CTR-ISO-004 explicitly;
    /// this is the one test in this project verifying that a cross-tenant request
    /// returns not-found rather than the record, at the Application layer.
    /// </summary>
    [Fact]
    public async Task Handle_ReturnsConnectorNotFound_ForACrossTenantRequest()
    {
        var connector = TestData.RegisteredConnector(tenantId: Guid.NewGuid());
        _repository.GetByIdAsync(connector.Id, Arg.Any<CancellationToken>()).Returns(connector);

        var result = await _handler.Handle(new GetConnectorQuery(connector.Id.Value, Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(IntegrationErrors.ConnectorNotFound);
    }
}
