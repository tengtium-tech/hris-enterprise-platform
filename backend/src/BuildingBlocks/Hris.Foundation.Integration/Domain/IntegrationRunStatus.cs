namespace Hris.Foundation.Integration.Domain;

/// <summary>
/// Source: docs/03-foundation/integration-framework.md, Error Handling ("Retry
/// Policies... Dead Letter Queue (DLQ)... Manual Replay") and Monitoring ("Successful
/// Integrations, Failed Integrations, Retry Count"). Mirrors Job Processing
/// Framework's own <c>JobStatus</c> shape for the identical Started/Completed/Failed/
/// DeadLetter core, collapsed to what one integration run actually needs (no separate
/// Queued/Scheduled step -- a run is dispatched directly, whether synchronously or via
/// a Job Processing Framework job the caller already submitted).
/// </summary>
public enum IntegrationRunStatus
{
    Started = 0,
    Completed,
    Failed,
    DeadLetter,
}
