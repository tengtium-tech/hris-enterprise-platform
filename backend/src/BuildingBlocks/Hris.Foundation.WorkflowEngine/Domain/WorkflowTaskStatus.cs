namespace Hris.Foundation.WorkflowEngine.Domain;

/// <summary>
/// One <see cref="WorkflowTask"/>'s own outcome. <see cref="Escalated"/> is terminal
/// for the task it is set on, not a paused/waiting state -- per
/// <see cref="WorkflowTask.Escalate"/>'s own remarks, escalating closes the current task
/// and a new <see cref="WorkflowTask"/> is created for the escalation target, matching
/// workflow-engine.md's own Example Escalation Chain, which names a new target at each
/// step rather than the same task waiting under a relabeled status.
/// </summary>
public enum WorkflowTaskStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Delegated = 3,
    Escalated = 4,
    Expired = 5,
    Cancelled = 6,
}
