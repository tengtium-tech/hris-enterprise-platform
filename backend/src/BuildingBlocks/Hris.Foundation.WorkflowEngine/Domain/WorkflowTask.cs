using Hris.SharedKernel;

namespace Hris.Foundation.WorkflowEngine.Domain;

/// <summary>
/// Aggregate Root for one unit of work assigned to a participant, per
/// workflow-engine.md's own Core Concepts ("A Workflow Task is assigned to a user or
/// role for action") -- see <see cref="WorkflowTaskId"/>'s own remarks for why this is
/// a separate Aggregate Root from <see cref="WorkflowInstance"/>, not a child Entity of
/// it.
///
/// <see cref="Delegate"/> is scoped to this one task only -- a one-time redirect, not
/// the standing "Temporary/Permanent/Out-of-Office Delegation, Effective Dates" rule
/// registry workflow-engine.md's own Delegation section separately describes, which
/// would auto-apply to every future task assigned to a user rather than one already-
/// created task. That standing-rule registry is deliberately out of this Sprint's own
/// scope (<c>DependencyInjection.cs</c>'s own remarks) -- a real, separate concept this
/// build does not invent a shape for ahead of a dedicated pass.
/// </summary>
public sealed class WorkflowTask : AggregateRoot<WorkflowTaskId>
{
    public Guid TenantId { get; }

    public WorkflowInstanceId WorkflowInstanceId { get; }

    public string StepName { get; }

    public int StepOrder { get; }

    public WorkflowParticipantType ParticipantType { get; }

    public string? ParticipantRoleName { get; }

    public Guid? AssignedToUserId { get; private set; }

    public WorkflowTaskStatus Status { get; private set; }

    public string? Comments { get; private set; }

    public Guid? DelegatedToUserId { get; private set; }

    public int EscalationLevel { get; }

    public DateTimeOffset CreatedAtUtc { get; }

    public DateTimeOffset? CompletedAtUtc { get; private set; }

    private WorkflowTask(
        WorkflowTaskId id,
        Guid tenantId,
        WorkflowInstanceId workflowInstanceId,
        string stepName,
        int stepOrder,
        WorkflowParticipantType participantType,
        string? participantRoleName,
        Guid? assignedToUserId,
        int escalationLevel,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        TenantId = tenantId;
        WorkflowInstanceId = workflowInstanceId;
        StepName = stepName;
        StepOrder = stepOrder;
        ParticipantType = participantType;
        ParticipantRoleName = participantRoleName;
        AssignedToUserId = assignedToUserId;
        Status = WorkflowTaskStatus.Pending;
        EscalationLevel = escalationLevel;
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>
    /// Assigns a new task, in <see cref="WorkflowTaskStatus.Pending"/>. Raises
    /// <see cref="WorkflowAssigned"/>. <paramref name="assignedToUserId"/> is
    /// <c>null</c> when <paramref name="participantType"/> is
    /// <see cref="WorkflowParticipantType.Role"/> and resolution to a concrete acting
    /// user is left to Authorization Framework's own claim-time evaluation -- a real
    /// Upstream Dependency with no concrete integration point yet
    /// (<c>DependencyInjection.cs</c>'s own remarks), not a gap unique to this
    /// aggregate.
    /// </summary>
    public static Result<WorkflowTask> Create(
        Guid tenantId,
        WorkflowInstanceId workflowInstanceId,
        string? stepName,
        int stepOrder,
        WorkflowParticipantType participantType,
        string? participantRoleName,
        Guid? assignedToUserId,
        int escalationLevel,
        DateTimeOffset createdAtUtc)
    {
        Guard.AgainstDefault(tenantId, nameof(tenantId));

        if (string.IsNullOrWhiteSpace(stepName))
        {
            return Result.Failure<WorkflowTask>(WorkflowEngineErrors.DefinitionNameRequired);
        }

        var task = new WorkflowTask(
            new WorkflowTaskId(Guid.NewGuid()), tenantId, workflowInstanceId, stepName.Trim(), stepOrder,
            participantType, participantRoleName, assignedToUserId, escalationLevel, createdAtUtc);

        task.AddDomainEvent(new WorkflowAssigned(Guid.NewGuid(), createdAtUtc, task.Id, workflowInstanceId, tenantId));
        return Result.Success(task);
    }

    public Result Approve(string? comments, DateTimeOffset nowUtc)
    {
        if (Status != WorkflowTaskStatus.Pending)
        {
            return Result.Failure(WorkflowEngineErrors.InvalidTaskLifecycleTransition);
        }

        Status = WorkflowTaskStatus.Approved;
        Comments = string.IsNullOrWhiteSpace(comments) ? null : comments.Trim();
        CompletedAtUtc = nowUtc;
        AddDomainEvent(new WorkflowApproved(Guid.NewGuid(), nowUtc, Id, WorkflowInstanceId));
        return Result.Success();
    }

    /// <summary>
    /// Raises no event of its own -- <see cref="WorkflowRejected"/> is
    /// <see cref="WorkflowInstance.Reject"/>'s own event, at the instance-level
    /// outcome, per <c>WorkflowEngineEvents</c>'s own remarks on avoiding recording the
    /// same rejection twice.
    /// </summary>
    public Result Reject(string? comments, DateTimeOffset nowUtc)
    {
        if (Status != WorkflowTaskStatus.Pending)
        {
            return Result.Failure(WorkflowEngineErrors.InvalidTaskLifecycleTransition);
        }

        Status = WorkflowTaskStatus.Rejected;
        Comments = string.IsNullOrWhiteSpace(comments) ? null : comments.Trim();
        CompletedAtUtc = nowUtc;
        return Result.Success();
    }

    public Result Delegate(Guid delegateToUserId, string? reason, DateTimeOffset nowUtc)
    {
        if (Status != WorkflowTaskStatus.Pending)
        {
            return Result.Failure(WorkflowEngineErrors.InvalidTaskLifecycleTransition);
        }

        if (delegateToUserId == Guid.Empty)
        {
            return Result.Failure(WorkflowEngineErrors.DelegateToUserRequired);
        }

        Status = WorkflowTaskStatus.Delegated;
        DelegatedToUserId = delegateToUserId;
        Comments = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        CompletedAtUtc = nowUtc;
        AddDomainEvent(new WorkflowDelegated(Guid.NewGuid(), nowUtc, Id, delegateToUserId));
        return Result.Success();
    }

    /// <summary>
    /// Closes this task as escalated -- terminal for this task specifically, per
    /// <see cref="WorkflowTaskStatus.Escalated"/>'s own remarks. The caller creates a
    /// new <see cref="WorkflowTask"/> for the escalation target in the same operation
    /// (<c>EscalateWorkflowTaskCommand</c>'s own handler), with
    /// <see cref="EscalationLevel"/> one higher than this task's own -- matching
    /// workflow-engine.md's own Example Escalation Chain, which names a new target at
    /// each step rather than mutating one waiting task in place.
    /// </summary>
    public Result Escalate(DateTimeOffset nowUtc)
    {
        if (Status != WorkflowTaskStatus.Pending)
        {
            return Result.Failure(WorkflowEngineErrors.InvalidTaskLifecycleTransition);
        }

        Status = WorkflowTaskStatus.Escalated;
        CompletedAtUtc = nowUtc;
        AddDomainEvent(new WorkflowEscalated(Guid.NewGuid(), nowUtc, Id, WorkflowInstanceId, EscalationLevel));
        return Result.Success();
    }

    /// <summary>
    /// An SLA breach with no response -- workflow-engine.md's own Escalation section
    /// ("SLA Expiration"). Raises no event: this document's Domain Events list names no
    /// "WorkflowExpired" event, the same asymmetry <c>WorkflowInstance.Expire</c>'s own
    /// remarks state for itself.
    /// </summary>
    public Result Expire(DateTimeOffset nowUtc)
    {
        if (Status != WorkflowTaskStatus.Pending)
        {
            return Result.Failure(WorkflowEngineErrors.InvalidTaskLifecycleTransition);
        }

        Status = WorkflowTaskStatus.Expired;
        CompletedAtUtc = nowUtc;
        return Result.Success();
    }

    /// <summary>
    /// The owning <see cref="WorkflowInstance"/> was cancelled or withdrawn -- this
    /// task's own outcome follows, since a task cannot remain actionable once the
    /// process it belongs to has ended. Raises no event of its own; the instance-level
    /// <see cref="WorkflowCancelled"/> already records why.
    /// </summary>
    public Result Cancel(DateTimeOffset nowUtc)
    {
        if (Status != WorkflowTaskStatus.Pending)
        {
            return Result.Failure(WorkflowEngineErrors.InvalidTaskLifecycleTransition);
        }

        Status = WorkflowTaskStatus.Cancelled;
        CompletedAtUtc = nowUtc;
        return Result.Success();
    }
}
