namespace Hris.Foundation.WorkflowEngine.Domain;

/// <summary>
/// Repository interface in the Domain layer, implementation in Infrastructure, per
/// repositories.md's own split.
/// </summary>
public interface IWorkflowInstanceRepository
{
    Task<WorkflowInstance?> GetByIdAsync(WorkflowInstanceId id, CancellationToken cancellationToken);

    /// <summary>
    /// workflow-engine.md's own Permissions table: "View all workflow instances,"
    /// scoped to <paramref name="tenantId"/> per CTR-ISO-004, ordered most recent
    /// first for audit/browsing -- the identical history-query shape every other
    /// Sprint 4/5 framework's own "ListXHistoryAsync" repository method already
    /// establishes.
    /// </summary>
    Task<IReadOnlyList<WorkflowInstance>> ListByDefinitionAsync(
        WorkflowDefinitionId workflowDefinitionId, Guid tenantId, CancellationToken cancellationToken);

    Task AddAsync(WorkflowInstance instance, CancellationToken cancellationToken);
}
