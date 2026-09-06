namespace Hris.Foundation.Integration.Application.Dtos;

/// <summary>
/// The read-side shape <c>GetIntegrationRunQuery</c>/<c>ListIntegrationRunHistoryQuery</c>
/// return, per dto-design.md's own convention.
/// </summary>
public sealed record IntegrationRunDto(
    Guid IntegrationRunId,
    Guid TenantId,
    Guid ConnectorId,
    string RunKind,
    string? SyncModel,
    Guid? JobId,
    string Status,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    int? RecordsProcessed,
    string? FailureReason,
    int RetryCount,
    int MaxRetries);
