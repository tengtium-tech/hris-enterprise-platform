namespace Hris.Foundation.WorkflowEngine.Domain;

/// <summary>
/// Repository interface in the Domain layer, implementation in Infrastructure, per
/// repositories.md's own split.
/// </summary>
public interface IWorkflowDefinitionRepository
{
    Task<WorkflowDefinition?> GetByIdAsync(WorkflowDefinitionId id, CancellationToken cancellationToken);

    Task<IReadOnlyList<WorkflowDefinition>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken);

    Task AddAsync(WorkflowDefinition definition, CancellationToken cancellationToken);
}
