using Hris.SharedKernel;

namespace Hris.Foundation.WorkflowEngine.Domain;

/// <summary>
/// Aggregate Root for one running execution of a <see cref="WorkflowDefinition"/>, per
/// workflow-engine.md's own Core Concepts ("Each request creates its own workflow
/// instance") and Workflow Lifecycle diagram. Population-scale sibling to
/// <see cref="WorkflowDefinition"/>, matching this Sprint's own <c>JobQueue</c>/<c>Job</c>
/// and <c>Schedule</c>/<c>ScheduleExecution</c> config-plus-occurrence split.
///
/// <see cref="WorkflowDefinitionVersionNumber"/> is a snapshot taken once at
/// <see cref="Trigger"/> time, per <see cref="WorkflowDefinition.GetPublishedVersion"/>'s
/// own remarks -- this aggregate never re-reads its own defining
/// <see cref="WorkflowDefinition"/> to discover "the current version," so republishing
/// a newer version never changes what an already-running instance is doing.
///
/// Every constructor parameter shares its name with the property it sets
/// (<c>startedAtUtc</c> -&gt; <see cref="StartedAtUtc"/>, not a differently-named
/// <c>nowUtc</c>), the proactive naming discipline every Sprint 4/5 aggregate after
/// Search Framework already establishes, confirmed by a real EF Core model build
/// needing no second constructor.
/// </summary>
public sealed class WorkflowInstance : AggregateRoot<WorkflowInstanceId>
{
    public Guid TenantId { get; }

    public WorkflowDefinitionId WorkflowDefinitionId { get; }

    public int WorkflowDefinitionVersionNumber { get; }

    public string? TriggeringReference { get; }

    public Guid InitiatedByUserId { get; }

    public WorkflowInstanceStatus Status { get; private set; }

    public int CurrentStepOrder { get; private set; }

    public DateTimeOffset StartedAtUtc { get; }

    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public string? FailureReason { get; private set; }

    private WorkflowInstance(
        WorkflowInstanceId id,
        Guid tenantId,
        WorkflowDefinitionId workflowDefinitionId,
        int workflowDefinitionVersionNumber,
        string? triggeringReference,
        Guid initiatedByUserId,
        DateTimeOffset startedAtUtc)
        : base(id)
    {
        TenantId = tenantId;
        WorkflowDefinitionId = workflowDefinitionId;
        WorkflowDefinitionVersionNumber = workflowDefinitionVersionNumber;
        TriggeringReference = triggeringReference;
        InitiatedByUserId = initiatedByUserId;
        Status = WorkflowInstanceStatus.Submitted;
        CurrentStepOrder = 0;
        StartedAtUtc = startedAtUtc;
    }

    /// <summary>
    /// Creates a new instance directly in <see cref="WorkflowInstanceStatus.Submitted"/>
    /// -- the same "Requested is never independently observable" reasoning
    /// <c>Tenant</c>'s own remarks give for its own analogous first state. Raises
    /// <see cref="WorkflowStarted"/>. The caller resolves and passes
    /// <paramref name="workflowDefinitionVersionNumber"/> from
    /// <see cref="WorkflowDefinition.GetPublishedVersion"/> before calling this factory
    /// -- this aggregate never loads a <see cref="WorkflowDefinition"/> for itself,
    /// cross-aggregate data it structurally cannot see.
    /// </summary>
    public static Result<WorkflowInstance> Trigger(
        Guid tenantId,
        WorkflowDefinitionId workflowDefinitionId,
        int workflowDefinitionVersionNumber,
        string? triggeringReference,
        Guid initiatedByUserId,
        DateTimeOffset startedAtUtc)
    {
        Guard.AgainstDefault(tenantId, nameof(tenantId));
        Guard.AgainstDefault(initiatedByUserId, nameof(initiatedByUserId));

        var instance = new WorkflowInstance(
            new WorkflowInstanceId(Guid.NewGuid()), tenantId, workflowDefinitionId, workflowDefinitionVersionNumber,
            string.IsNullOrWhiteSpace(triggeringReference) ? null : triggeringReference.Trim(), initiatedByUserId, startedAtUtc);

        instance.AddDomainEvent(new WorkflowStarted(
            Guid.NewGuid(), startedAtUtc, instance.Id, tenantId, workflowDefinitionId, workflowDefinitionVersionNumber));

        return Result.Success(instance);
    }

    /// <summary>
    /// Advances to the given step. The first call moves
    /// <see cref="WorkflowInstanceStatus.Submitted"/> -&gt;
    /// <see cref="WorkflowInstanceStatus.InProgress"/> and raises
    /// <see cref="WorkflowSubmitted"/>, per <c>WorkflowEngineEvents</c>'s own remarks on
    /// why that event is told apart from <see cref="WorkflowStarted"/> here; later calls
    /// stay <see cref="WorkflowInstanceStatus.InProgress"/> and raise nothing further --
    /// workflow-engine.md's own Domain Events list names no per-step-advance event.
    /// </summary>
    public Result Advance(int nextStepOrder, DateTimeOffset nowUtc)
    {
        if (Status is not (WorkflowInstanceStatus.Submitted or WorkflowInstanceStatus.InProgress or WorkflowInstanceStatus.PendingApproval))
        {
            return Result.Failure(WorkflowEngineErrors.InvalidInstanceLifecycleTransition);
        }

        var wasSubmitted = Status == WorkflowInstanceStatus.Submitted;

        Status = WorkflowInstanceStatus.InProgress;
        CurrentStepOrder = nextStepOrder;

        if (wasSubmitted)
        {
            AddDomainEvent(new WorkflowSubmitted(Guid.NewGuid(), nowUtc, Id));
        }

        return Result.Success();
    }

    /// <summary>
    /// Reached when <see cref="CurrentStepOrder"/> is an Approval step -- the caller
    /// creates the corresponding <see cref="WorkflowTask"/> separately (this aggregate
    /// never creates one directly, the same cross-aggregate boundary
    /// <see cref="Trigger"/>'s own remarks describe for reading a
    /// <see cref="WorkflowDefinition"/>).
    /// </summary>
    public Result RequestApproval()
    {
        if (Status != WorkflowInstanceStatus.InProgress)
        {
            return Result.Failure(WorkflowEngineErrors.InvalidInstanceLifecycleTransition);
        }

        Status = WorkflowInstanceStatus.PendingApproval;
        return Result.Success();
    }

    /// <summary>
    /// The corresponding <see cref="WorkflowTask"/> approved -- resumes processing.
    /// Raises no event of its own: <see cref="WorkflowApproved"/> is
    /// <see cref="WorkflowTask.Approve"/>'s own event, at the individual-task level;
    /// this method only unblocks the instance to continue toward its own
    /// <see cref="Complete"/>.
    /// </summary>
    public Result ResumeAfterApproval(int nextStepOrder, DateTimeOffset nowUtc)
    {
        if (Status != WorkflowInstanceStatus.PendingApproval)
        {
            return Result.Failure(WorkflowEngineErrors.InvalidInstanceLifecycleTransition);
        }

        Status = WorkflowInstanceStatus.InProgress;
        CurrentStepOrder = nextStepOrder;
        return Result.Success();
    }

    public Result Reject(string? reason, DateTimeOffset nowUtc)
    {
        if (Status != WorkflowInstanceStatus.PendingApproval)
        {
            return Result.Failure(WorkflowEngineErrors.InvalidInstanceLifecycleTransition);
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result.Failure(WorkflowEngineErrors.ReasonRequired);
        }

        Status = WorkflowInstanceStatus.Rejected;
        CompletedAtUtc = nowUtc;
        AddDomainEvent(new WorkflowRejected(Guid.NewGuid(), nowUtc, Id, reason.Trim()));
        return Result.Success();
    }

    /// <summary>
    /// Any non-terminal state, per workflow-engine.md's own Permissions table
    /// ("Cancel a running instance") naming no state restriction of its own -- the same
    /// broad multi-state guard <c>IssuedNumber.Release</c>'s and <c>Schedule.Retire</c>'s
    /// own remarks justify for an administrative override that must always be callable.
    /// </summary>
    public Result Cancel(string? reason, DateTimeOffset nowUtc)
    {
        if (IsTerminal())
        {
            return Result.Failure(WorkflowEngineErrors.InvalidInstanceLifecycleTransition);
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result.Failure(WorkflowEngineErrors.ReasonRequired);
        }

        Status = WorkflowInstanceStatus.Cancelled;
        CompletedAtUtc = nowUtc;
        AddDomainEvent(new WorkflowCancelled(Guid.NewGuid(), nowUtc, Id, reason.Trim()));
        return Result.Success();
    }

    /// <summary>
    /// The initiator withdraws their own request before it reaches a final outcome.
    /// Raises no event: workflow-engine.md's own Domain Events list names no
    /// "WorkflowWithdrawn" event, the same asymmetry already accepted elsewhere in this
    /// framework for a status this document's own Lifecycle diagram names but its own
    /// Domain Events list does not.
    /// </summary>
    public Result Withdraw(DateTimeOffset nowUtc)
    {
        if (IsTerminal())
        {
            return Result.Failure(WorkflowEngineErrors.InvalidInstanceLifecycleTransition);
        }

        Status = WorkflowInstanceStatus.Withdrawn;
        CompletedAtUtc = nowUtc;
        return Result.Success();
    }

    /// <summary>
    /// An SLA breach with no resolution -- see <see cref="WorkflowTask.Expire"/> for the
    /// task-level counterpart this instance-level transition normally follows. Raises
    /// no event, the same asymmetry <see cref="Withdraw"/>'s own remarks state.
    /// </summary>
    public Result Expire(DateTimeOffset nowUtc)
    {
        if (Status != WorkflowInstanceStatus.PendingApproval)
        {
            return Result.Failure(WorkflowEngineErrors.InvalidInstanceLifecycleTransition);
        }

        Status = WorkflowInstanceStatus.Expired;
        CompletedAtUtc = nowUtc;
        return Result.Success();
    }

    /// <summary>
    /// Terminal failure -- workflow-engine.md's own Error Handling and Resilience
    /// section: "A workflow instance must never be silently abandoned. Terminal failure
    /// raises an administrative notification." Raising that administrative
    /// notification is Notification Framework's own concern once wired
    /// (<c>DependencyInjection.cs</c>'s own remarks); this method only records the
    /// terminal state itself. Raises no bespoke event of its own -- the same asymmetry
    /// <see cref="Withdraw"/>'s own remarks state, this document's Domain Events list
    /// naming no "WorkflowFailed" event either.
    /// </summary>
    public Result Fail(string? reason, DateTimeOffset nowUtc)
    {
        if (IsTerminal())
        {
            return Result.Failure(WorkflowEngineErrors.InvalidInstanceLifecycleTransition);
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result.Failure(WorkflowEngineErrors.ReasonRequired);
        }

        Status = WorkflowInstanceStatus.Failed;
        CompletedAtUtc = nowUtc;
        FailureReason = reason.Trim();
        return Result.Success();
    }

    public Result Complete(DateTimeOffset nowUtc)
    {
        if (Status is not (WorkflowInstanceStatus.InProgress or WorkflowInstanceStatus.Approved))
        {
            return Result.Failure(WorkflowEngineErrors.InvalidInstanceLifecycleTransition);
        }

        Status = WorkflowInstanceStatus.Completed;
        CompletedAtUtc = nowUtc;
        AddDomainEvent(new WorkflowCompleted(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    private bool IsTerminal() => Status is WorkflowInstanceStatus.Completed or WorkflowInstanceStatus.Rejected
        or WorkflowInstanceStatus.Cancelled or WorkflowInstanceStatus.Expired or WorkflowInstanceStatus.Withdrawn
        or WorkflowInstanceStatus.Failed;
}
