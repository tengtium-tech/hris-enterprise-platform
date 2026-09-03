using Hris.SharedKernel;

namespace Hris.Foundation.JobProcessing.Domain;

/// <summary>
/// Aggregate Root for one registered worker process instance -- job-processing.md's
/// own Core Concepts ("Workers execute queued jobs") and Monitoring section ("Worker
/// Health"). Deliberately minimal: registration start/stop only. Actual health-check
/// heartbeats, auto-scaling, and graceful-shutdown coordination are real
/// operational/hosting concerns outside this Sprint's own Scope, see
/// <c>DependencyInjection.cs</c>'s own remarks -- the same "record the fact, do not
/// build the operational machinery behind it" split <see cref="JobQueue"/>'s own
/// remarks draw for <see cref="JobQueue.MaxConcurrency"/>.
///
/// No <c>TenantId</c>: a worker process is platform infrastructure serving every
/// tenant, not data scoped to one, the same platform-vs-tenant boundary
/// ADR-0009 draws for platform-operator capabilities.
/// </summary>
public sealed class Worker : AggregateRoot<WorkerId>
{
    public string InstanceId { get; }

    public WorkerStatus Status { get; private set; }

    public DateTimeOffset StartedAtUtc { get; }

    public DateTimeOffset? StoppedAtUtc { get; private set; }

    private Worker(WorkerId id, string instanceId, WorkerStatus status, DateTimeOffset startedAtUtc, DateTimeOffset? stoppedAtUtc)
        : base(id)
    {
        InstanceId = instanceId;
        Status = status;
        StartedAtUtc = startedAtUtc;
        StoppedAtUtc = stoppedAtUtc;
    }

    public static Result<Worker> Start(string? instanceId, DateTimeOffset nowUtc)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
        {
            return Result.Failure<Worker>(JobProcessingErrors.WorkerInstanceIdRequired);
        }

        var trimmedInstanceId = instanceId.Trim();
        var worker = new Worker(new WorkerId(Guid.NewGuid()), trimmedInstanceId, WorkerStatus.Running, nowUtc, stoppedAtUtc: null);

        worker.AddDomainEvent(new WorkerStarted(Guid.NewGuid(), nowUtc, worker.Id, trimmedInstanceId));
        return Result.Success(worker);
    }

    public Result Stop(DateTimeOffset nowUtc)
    {
        if (Status != WorkerStatus.Running)
        {
            return Result.Failure(JobProcessingErrors.InvalidWorkerLifecycleTransition);
        }

        Status = WorkerStatus.Stopped;
        StoppedAtUtc = nowUtc;
        AddDomainEvent(new WorkerStopped(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }
}
