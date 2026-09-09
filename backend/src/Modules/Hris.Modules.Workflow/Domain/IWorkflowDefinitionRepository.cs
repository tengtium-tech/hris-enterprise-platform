namespace Hris.Modules.Workflow.Domain;

/// <summary>
/// Repository contract for the <see cref="WorkflowDefinition"/> Aggregate Root.
/// There is deliberately no repository for <see cref="WorkflowStep"/>: a step is
/// reached only through its definition (CTR-ARC-004), because publication validates
/// the graph as a whole and a separately persistable step would let a published
/// definition be edited into an invalid one with nothing checking.
/// </summary>
public interface IWorkflowDefinitionRepository
{
    Task<WorkflowDefinition?> GetByIdAsync(WorkflowDefinitionId id, CancellationToken cancellationToken);

    Task<IReadOnlyList<WorkflowDefinition>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken);

    /// <summary>
    /// Every version in one lineage, current and superseded. Superseded versions are
    /// retained rather than deleted, because instances that started under them may
    /// still be running and workflow-versioning.md's own guarantee is only checkable
    /// if they remain queryable.
    /// </summary>
    Task<IReadOnlyList<WorkflowDefinition>> ListByLineageAsync(Guid lineageId, CancellationToken cancellationToken);

    /// <summary>WR-006: name uniqueness within a tenant, per business process.</summary>
    Task<bool> NameExistsForBusinessProcessAsync(
        Guid tenantId, Guid businessProcessId, string name, WorkflowDefinitionId? excluding, CancellationToken cancellationToken);

    Task AddAsync(WorkflowDefinition definition, CancellationToken cancellationToken);
}
