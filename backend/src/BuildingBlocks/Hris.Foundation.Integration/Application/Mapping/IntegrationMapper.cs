using Hris.Foundation.Integration.Application.Dtos;
using Hris.Foundation.Integration.Domain;
using Hris.SharedKernel;

namespace Hris.Foundation.Integration.Application.Mapping;

/// <summary>
/// Domain-to-DTO mapping, kept as a plain static class rather than a library such as
/// AutoMapper, per mapping.md's own stated preference for explicit mapping code -- the
/// identical choice every other framework's own mapper already establishes. Also
/// carries this framework's own wire-format-string parsers, the same "carries raw
/// primitives, not Domain Value Objects, across the MediatR boundary" choice
/// <c>RequestFileUploadCommand</c>'s own remarks state for itself.
/// </summary>
internal static class IntegrationMapper
{
    public static Result<EndpointType> ParseEndpointType(string? value) =>
        Enum.TryParse<EndpointType>(value, ignoreCase: true, out var endpointType) && Enum.IsDefined(endpointType)
            ? Result.Success(endpointType)
            : Result.Failure<EndpointType>(IntegrationErrors.InvalidEndpointType);

    public static Result<TransformationType> ParseTransformationType(string? value) =>
        Enum.TryParse<TransformationType>(value, ignoreCase: true, out var transformationType) && Enum.IsDefined(transformationType)
            ? Result.Success(transformationType)
            : Result.Failure<TransformationType>(IntegrationErrors.InvalidTransformationType);

    public static Result<IntegrationRunKind> ParseRunKind(string? value) =>
        Enum.TryParse<IntegrationRunKind>(value, ignoreCase: true, out var runKind) && Enum.IsDefined(runKind)
            ? Result.Success(runKind)
            : Result.Failure<IntegrationRunKind>(IntegrationErrors.InvalidRunKind);

    public static ConnectorDto ToDto(Connector connector) => new(
        connector.Id.Value,
        connector.TenantId,
        connector.Name,
        connector.IntegrationCategory,
        connector.EndpointType.ToString(),
        connector.EndpointAddress,
        connector.Status.ToString(),
        connector.CreatedAtUtc,
        connector.Mappings.Select(ToDto).ToList());

    public static DataMappingDto ToDto(DataMapping mapping) => new(
        mapping.Id.Value,
        mapping.SourceField,
        mapping.TargetField,
        mapping.TransformationType.ToString(),
        mapping.TransformationRule);

    public static ConnectorSummaryDto ToSummaryDto(Connector connector) => new(
        connector.Id.Value,
        connector.Name,
        connector.IntegrationCategory,
        connector.EndpointType.ToString(),
        connector.Status.ToString());

    public static IntegrationRunDto ToDto(IntegrationRun run) => new(
        run.Id.Value,
        run.TenantId,
        run.ConnectorId.Value,
        run.RunKind.ToString(),
        run.SyncModel,
        run.JobId,
        run.Status.ToString(),
        run.StartedAtUtc,
        run.CompletedAtUtc,
        run.RecordsProcessed,
        run.FailureReason,
        run.RetryCount,
        run.MaxRetries);
}
