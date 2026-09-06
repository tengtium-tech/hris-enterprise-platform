namespace Hris.Foundation.Integration.Application.Dtos;

/// <summary>
/// The read-side shape <c>GetConnectorQuery</c> returns, per dto-design.md's own
/// convention.
/// </summary>
public sealed record ConnectorDto(
    Guid ConnectorId,
    Guid TenantId,
    string Name,
    string IntegrationCategory,
    string EndpointType,
    string EndpointAddress,
    string Status,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<DataMappingDto> Mappings);

public sealed record DataMappingDto(
    Guid DataMappingId,
    string SourceField,
    string TargetField,
    string TransformationType,
    string? TransformationRule);

/// <summary>
/// The lighter-weight shape <c>ListConnectorsQuery</c> returns per matching row --
/// api-standards.md's own Response Shapes guidance that a list endpoint returns a
/// summary projection, not every field the single-resource endpoint does.
/// </summary>
public sealed record ConnectorSummaryDto(
    Guid ConnectorId,
    string Name,
    string IntegrationCategory,
    string EndpointType,
    string Status);
