using Hris.Foundation.WorkflowEngine.Domain;
using Hris.Infrastructure.Persistence;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hris.Foundation.WorkflowEngine.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IWorkflowDefinitionRepository"/>, per
/// repositories.md's "interface in the Domain layer... implementation in
/// Infrastructure" split.
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

    public async Task<IReadOnlyList<WorkflowDefinition>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken) =>
        await _dbContext.Set<WorkflowDefinition>()
            .Where(definition => definition.TenantId == tenantId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task AddAsync(WorkflowDefinition definition, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(definition, nameof(definition));
        await _dbContext.Set<WorkflowDefinition>().AddAsync(definition, cancellationToken).ConfigureAwait(false);
    }
}
