namespace Hris.Foundation.WorkflowEngine.Domain;

/// <summary>
/// Repository interface in the Domain layer, implementation in Infrastructure, per
/// repositories.md's own split.
/// </summary>
public interface IWorkflowTaskRepository
{
    Task<WorkflowTask?> GetByIdAsync(WorkflowTaskId id, CancellationToken cancellationToken);

    Task<IReadOnlyList<WorkflowTask>> ListByInstanceAsync(WorkflowInstanceId workflowInstanceId, CancellationToken cancellationToken);

    /// <summary>
    /// workflow-engine.md's own Permissions table: "Act on assigned approval," scoped
    /// to the caller's own <paramref name="assignedToUserId"/> within
    /// <paramref name="tenantId"/> per CTR-ISO-004 -- the concrete query a caller's own
    /// task inbox reads.
    /// </summary>
    Task<IReadOnlyList<WorkflowTask>> ListByAssigneeAsync(
        Guid assignedToUserId, Guid tenantId, CancellationToken cancellationToken);

    Task AddAsync(WorkflowTask task, CancellationToken cancellationToken);
}
