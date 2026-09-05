using Hris.Foundation.WorkflowEngine.Domain;
using Hris.Infrastructure.Persistence;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hris.Foundation.WorkflowEngine.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IWorkflowInstanceRepository"/>, per
/// repositories.md's "interface in the Domain layer... implementation in
/// Infrastructure" split.
/// </summary>
internal sealed class WorkflowInstanceRepository : IWorkflowInstanceRepository
{
    private readonly HrisDbContext _dbContext;

    public WorkflowInstanceRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public Task<WorkflowInstance?> GetByIdAsync(WorkflowInstanceId id, CancellationToken cancellationToken) =>
        _dbContext.Set<WorkflowInstance>().FirstOrDefaultAsync(instance => instance.Id == id, cancellationToken);

    public async Task<IReadOnlyList<WorkflowInstance>> ListByDefinitionAsync(
        WorkflowDefinitionId workflowDefinitionId, Guid tenantId, CancellationToken cancellationToken) =>
        await _dbContext.Set<WorkflowInstance>()
            .Where(instance => instance.WorkflowDefinitionId == workflowDefinitionId && instance.TenantId == tenantId)
            .OrderByDescending(instance => instance.StartedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task AddAsync(WorkflowInstance instance, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(instance, nameof(instance));
        await _dbContext.Set<WorkflowInstance>().AddAsync(instance, cancellationToken).ConfigureAwait(false);
    }
}
