using Hris.SharedKernel;

namespace Hris.Foundation.Integration.Domain;

/// <summary>
/// integration-framework.md's own Domain Events section names exactly nine events --
/// every one implemented here, split two/seven across <see cref="Connector"/> and
/// <see cref="IntegrationRun"/>. <see cref="IntegrationRun"/>'s own
/// <see cref="IntegrationRunKind"/> decides which pair a given run raises on
/// <see cref="IntegrationRun.Start"/>/<see cref="IntegrationRun.Complete"/>:
/// <see cref="IntegrationRunKind.IntegrationCall"/> raises
/// <see cref="IntegrationStarted"/>/<see cref="IntegrationCompleted"/>,
/// <see cref="IntegrationRunKind.Synchronization"/> raises
/// <see cref="SynchronizationStarted"/>/<see cref="SynchronizationCompleted"/>, and
/// <see cref="IntegrationRunKind.WebhookDelivery"/> raises
/// <see cref="IntegrationStarted"/> (the document names no distinct "WebhookStarted")
/// on start but <see cref="WebhookDelivered"/> -- not a generic "completed" event --
/// on completion, since that event name literally is this kind's own "success"
/// outcome. <see cref="IntegrationRun.Fail"/> always raises
/// <see cref="IntegrationFailed"/> regardless of kind: the document names no
/// per-kind failure event (no "SynchronizationFailed" or "WebhookFailed"), the same
/// asymmetric-event-list pattern every other framework in this codebase already
/// follows. <see cref="IntegrationRun.MoveToDeadLetter"/> raises no event of its own
/// for the identical reason.
/// </summary>
public sealed record ConnectorRegistered(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    ConnectorId ConnectorId,
    Guid TenantId,
    string IntegrationCategory) : IDomainEvent;

public sealed record ConnectorUpdated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    ConnectorId ConnectorId) : IDomainEvent;

public sealed record IntegrationStarted(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    IntegrationRunId IntegrationRunId,
    Guid TenantId,
    ConnectorId ConnectorId) : IDomainEvent;

public sealed record IntegrationCompleted(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    IntegrationRunId IntegrationRunId,
    int RecordsProcessed) : IDomainEvent;

public sealed record IntegrationFailed(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    IntegrationRunId IntegrationRunId,
    string Reason) : IDomainEvent;

public sealed record SynchronizationStarted(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    IntegrationRunId IntegrationRunId,
    Guid TenantId,
    ConnectorId ConnectorId) : IDomainEvent;

public sealed record SynchronizationCompleted(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    IntegrationRunId IntegrationRunId,
    int RecordsProcessed) : IDomainEvent;

public sealed record WebhookDelivered(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    IntegrationRunId IntegrationRunId) : IDomainEvent;

public sealed record RetryInitiated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    IntegrationRunId IntegrationRunId,
    int RetryCount) : IDomainEvent;
