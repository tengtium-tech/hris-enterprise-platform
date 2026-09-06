using Hris.SharedKernel;

namespace Hris.Foundation.Integration.Domain;

/// <summary>
/// Aggregate Root recording one execution of a <see cref="Connector"/>'s own
/// integration logic. Source: docs/03-foundation/integration-framework.md, Monitoring
/// ("Successful Integrations, Failed Integrations, Retry Count") and Error Handling
/// ("Retry Policies... Dead Letter Queue (DLQ)... Manual Replay"). A separate,
/// population-scale Aggregate Root from <see cref="Connector"/>, mirroring the
/// identical "definition vs. execution history" split <c>Job</c>/<c>JobQueue</c> and
/// <c>Schedule</c>/<c>ScheduleExecution</c> already establish in this codebase --
/// this document's own Non-Functional Requirements state "millions of transactions
/// across multiple tenants."
///
/// <see cref="JobId"/> is a plain, optional, caller-supplied <see cref="Guid"/> --
/// this framework's own stated Sprint 4 dependency, Job Processing Framework, is not
/// a compile-time <c>ProjectReference</c> (per this codebase's own established
/// discipline; see <c>DependencyInjection.cs</c>'s own remarks), but a run that
/// actually executed as a Job Processing Framework background job (rather than
/// synchronously, inline) can still record which one, the same "real but
/// caller-supplied, not compile-time" cross-framework reference
/// <c>DocumentVersion.StoredFileId</c> already establishes.
/// </summary>
public sealed class IntegrationRun : AggregateRoot<IntegrationRunId>
{
    public Guid TenantId { get; }

    public ConnectorId ConnectorId { get; }

    public IntegrationRunKind RunKind { get; }

    public string? SyncModel { get; }

    public Guid? JobId { get; }

    public IntegrationRunStatus Status { get; private set; }

    public DateTimeOffset StartedAtUtc { get; private set; }

    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public int? RecordsProcessed { get; private set; }

    public string? FailureReason { get; private set; }

    public int RetryCount { get; private set; }

    public int MaxRetries { get; }

    private IntegrationRun(
        IntegrationRunId id,
        Guid tenantId,
        ConnectorId connectorId,
        IntegrationRunKind runKind,
        string? syncModel,
        Guid? jobId,
        int maxRetries,
        DateTimeOffset startedAtUtc)
        : base(id)
    {
        TenantId = tenantId;
        ConnectorId = connectorId;
        RunKind = runKind;
        SyncModel = syncModel;
        JobId = jobId;
        MaxRetries = maxRetries;
        Status = IntegrationRunStatus.Started;
        StartedAtUtc = startedAtUtc;
        RetryCount = 0;
    }

    /// <summary>
    /// Begins a new run, in <see cref="IntegrationRunStatus.Started"/>. Raises
    /// <see cref="IntegrationStarted"/> or <see cref="SynchronizationStarted"/>
    /// depending on <paramref name="runKind"/> -- see <see cref="IntegrationEvents"/>'s
    /// own remarks for the full mapping.
    /// </summary>
    public static Result<IntegrationRun> Start(
        Guid tenantId,
        ConnectorId connectorId,
        IntegrationRunKind runKind,
        string? syncModel,
        Guid? jobId,
        int maxRetries,
        DateTimeOffset nowUtc)
    {
        Guard.AgainstDefault(tenantId, nameof(tenantId));

        var run = new IntegrationRun(
            new IntegrationRunId(Guid.NewGuid()), tenantId, connectorId, runKind,
            string.IsNullOrWhiteSpace(syncModel) ? null : syncModel.Trim(), jobId, maxRetries, nowUtc);

        run.AddDomainEvent(runKind == IntegrationRunKind.Synchronization
            ? new SynchronizationStarted(Guid.NewGuid(), nowUtc, run.Id, tenantId, connectorId)
            : new IntegrationStarted(Guid.NewGuid(), nowUtc, run.Id, tenantId, connectorId));

        return Result.Success(run);
    }

    /// <summary>
    /// Raises <see cref="SynchronizationCompleted"/> for
    /// <see cref="IntegrationRunKind.Synchronization"/>, <see cref="WebhookDelivered"/>
    /// for <see cref="IntegrationRunKind.WebhookDelivery"/> (that event name is itself
    /// this kind's own success outcome, not a generic "completed" event), and
    /// <see cref="IntegrationCompleted"/> otherwise.
    /// </summary>
    public Result Complete(int recordsProcessed, DateTimeOffset nowUtc)
    {
        if (Status != IntegrationRunStatus.Started)
        {
            return Result.Failure(IntegrationErrors.InvalidIntegrationRunLifecycleTransition);
        }

        Status = IntegrationRunStatus.Completed;
        CompletedAtUtc = nowUtc;
        RecordsProcessed = recordsProcessed;

        AddDomainEvent(RunKind switch
        {
            IntegrationRunKind.Synchronization => new SynchronizationCompleted(Guid.NewGuid(), nowUtc, Id, recordsProcessed),
            IntegrationRunKind.WebhookDelivery => new WebhookDelivered(Guid.NewGuid(), nowUtc, Id),
            _ => new IntegrationCompleted(Guid.NewGuid(), nowUtc, Id, recordsProcessed),
        });

        return Result.Success();
    }

    /// <summary>
    /// Always raises <see cref="IntegrationFailed"/>, regardless of
    /// <see cref="RunKind"/> -- the document names no per-kind failure event (no
    /// "SynchronizationFailed"/"WebhookFailed"), the same asymmetric-event-list
    /// pattern every other framework in this codebase already follows.
    /// </summary>
    public Result Fail(string? reason, DateTimeOffset nowUtc)
    {
        if (Status != IntegrationRunStatus.Started)
        {
            return Result.Failure(IntegrationErrors.InvalidIntegrationRunLifecycleTransition);
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result.Failure(IntegrationErrors.FailureReasonRequired);
        }

        Status = IntegrationRunStatus.Failed;
        FailureReason = reason.Trim();
        AddDomainEvent(new IntegrationFailed(Guid.NewGuid(), nowUtc, Id, FailureReason));
        return Result.Success();
    }

    /// <summary>
    /// Returns this run to <see cref="IntegrationRunStatus.Started"/> for another
    /// attempt -- integration-framework.md's own Error Handling section ("Retry
    /// Policies... Exponential Backoff... Manual Replay"). Fails once
    /// <see cref="RetryCount"/> would reach <see cref="MaxRetries"/> -- the caller must
    /// route to <see cref="MoveToDeadLetter"/> instead, the identical
    /// never-loop-indefinitely reasoning <c>Job.Retry</c>'s own remarks already
    /// establish for itself.
    /// </summary>
    public Result Retry(DateTimeOffset nowUtc)
    {
        if (Status != IntegrationRunStatus.Failed)
        {
            return Result.Failure(IntegrationErrors.InvalidIntegrationRunLifecycleTransition);
        }

        if (RetryCount >= MaxRetries)
        {
            return Result.Failure(IntegrationErrors.RetryLimitExceeded);
        }

        RetryCount++;
        Status = IntegrationRunStatus.Started;
        StartedAtUtc = nowUtc;
        FailureReason = null;
        AddDomainEvent(new RetryInitiated(Guid.NewGuid(), nowUtc, Id, RetryCount));
        return Result.Success();
    }

    /// <summary>
    /// Moves an exhausted run to the Dead Letter Queue. Raises no event: the
    /// document's own Domain Events list names none for this terminal transition, the
    /// same asymmetry <c>Notification.MoveToDeadLetter</c>'s own remarks note are
    /// possible for a state a document names in prose (Error Handling's own "Dead
    /// Letter Queue (DLQ)") but not in its own event list.
    /// </summary>
    public Result MoveToDeadLetter()
    {
        if (Status != IntegrationRunStatus.Failed)
        {
            return Result.Failure(IntegrationErrors.InvalidIntegrationRunLifecycleTransition);
        }

        Status = IntegrationRunStatus.DeadLetter;
        return Result.Success();
    }
}
