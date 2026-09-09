using Hris.Infrastructure.Persistence;
using Hris.Modules.Workflow.Domain;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hris.Modules.Workflow.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IWorkflowDefinitionRepository"/>. Every read
/// includes the owned step collection, since a definition without its steps cannot
/// be validated or published and there is no separate step repository to load them
/// from.
/// </summary>
internal sealed class WorkflowDefinitionRepository : IWorkflowDefinitionRepository
{
    private readonly HrisDbContext _dbContext;

    public WorkflowDefinitionRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public Task<WorkflowDefinition?> GetByIdAsync(WorkflowDefinitionId id, CancellationToken cancellationToken) =>
        _dbContext.Set<WorkflowDefinition>().FirstOrDefaultAsync(definition => definition.Id == id, cancellationToken);

    public async Task<IReadOnlyList<WorkflowDefinition>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return await _dbContext.Set<WorkflowDefinition>()
            .Where(definition => definition.TenantId == tenantId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<WorkflowDefinition>> ListByLineageAsync(Guid lineageId, CancellationToken cancellationToken)
    {
        return await _dbContext.Set<WorkflowDefinition>()
            .Where(definition => definition.LineageId == lineageId)
            .OrderBy(definition => definition.Version)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> NameExistsForBusinessProcessAsync(
        Guid tenantId, Guid businessProcessId, string name, WorkflowDefinitionId? excluding, CancellationToken cancellationToken)
    {
        return await _dbContext.Set<WorkflowDefinition>()
            .Where(definition => definition.TenantId == tenantId)
            .Where(definition => definition.BusinessProcessId == businessProcessId)
            .Where(definition => definition.Name == name)
            .Where(definition => excluding == null || definition.Id != excluding)
            .AnyAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(WorkflowDefinition definition, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(definition, nameof(definition));
        await _dbContext.Set<WorkflowDefinition>().AddAsync(definition, cancellationToken).ConfigureAwait(false);
    }
}
