using Hris.Foundation.WorkflowEngine.Domain;
using Hris.Infrastructure.Persistence;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hris.Foundation.WorkflowEngine.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IWorkflowTaskRepository"/>, per
/// repositories.md's "interface in the Domain layer... implementation in
/// Infrastructure" split.
/// </summary>
internal sealed class WorkflowTaskRepository : IWorkflowTaskRepository
{
    private readonly HrisDbContext _dbContext;

    public WorkflowTaskRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public Task<WorkflowTask?> GetByIdAsync(WorkflowTaskId id, CancellationToken cancellationToken) =>
        _dbContext.Set<WorkflowTask>().FirstOrDefaultAsync(task => task.Id == id, cancellationToken);

    public async Task<IReadOnlyList<WorkflowTask>> ListByInstanceAsync(
        WorkflowInstanceId workflowInstanceId, CancellationToken cancellationToken) =>
        await _dbContext.Set<WorkflowTask>()
            .Where(task => task.WorkflowInstanceId == workflowInstanceId)
            .OrderBy(task => task.StepOrder)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<WorkflowTask>> ListByAssigneeAsync(
        Guid assignedToUserId, Guid tenantId, CancellationToken cancellationToken) =>
        await _dbContext.Set<WorkflowTask>()
            .Where(task => task.AssignedToUserId == assignedToUserId && task.TenantId == tenantId)
            .OrderByDescending(task => task.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task AddAsync(WorkflowTask task, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(task, nameof(task));
        await _dbContext.Set<WorkflowTask>().AddAsync(task, cancellationToken).ConfigureAwait(false);
    }
}
