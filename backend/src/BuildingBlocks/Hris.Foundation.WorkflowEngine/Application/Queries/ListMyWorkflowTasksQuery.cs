using Hris.Application.Abstractions;
using Hris.Foundation.WorkflowEngine.Application.Dtos;
using Hris.Foundation.WorkflowEngine.Application.Mapping;
using Hris.Foundation.WorkflowEngine.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.WorkflowEngine.Application.Queries;

/// <summary>
/// workflow-engine.md's own Permissions table: "Act on assigned approval." Scoped to
/// the caller's own <see cref="AssignedToUserId"/> within <see cref="TenantId"/>,
/// always -- the identical "no admin-override path that reads another user's own
/// assignment" discipline notification-framework.md's own <c>GetMyNotificationsQuery</c>
/// remarks establish for its own analogous self-scoped query.
/// </summary>
public sealed record ListMyWorkflowTasksQuery(Guid AssignedToUserId, Guid TenantId) : IQuery<Result<IReadOnlyList<WorkflowTaskDto>>>;

internal sealed class ListMyWorkflowTasksQueryHandler
    : IRequestHandler<ListMyWorkflowTasksQuery, Result<IReadOnlyList<WorkflowTaskDto>>>
{
    private readonly IWorkflowTaskRepository _repository;

    public ListMyWorkflowTasksQueryHandler(IWorkflowTaskRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<IReadOnlyList<WorkflowTaskDto>>> Handle(
        ListMyWorkflowTasksQuery request, CancellationToken cancellationToken)
    {
        var tasks = await _repository.ListByAssigneeAsync(
            request.AssignedToUserId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return Result.Success<IReadOnlyList<WorkflowTaskDto>>(tasks.Select(WorkflowEngineMapper.ToDto).ToList());
    }
}
