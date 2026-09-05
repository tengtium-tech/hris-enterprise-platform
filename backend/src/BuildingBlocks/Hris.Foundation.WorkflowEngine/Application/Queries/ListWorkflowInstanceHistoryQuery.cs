using Hris.Application.Abstractions;
using Hris.Foundation.WorkflowEngine.Application.Dtos;
using Hris.Foundation.WorkflowEngine.Application.Mapping;
using Hris.Foundation.WorkflowEngine.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.WorkflowEngine.Application.Queries;

public sealed record ListWorkflowInstanceHistoryQuery(
    Guid WorkflowDefinitionId, Guid TenantId) : IQuery<Result<IReadOnlyList<WorkflowInstanceDto>>>;

internal sealed class ListWorkflowInstanceHistoryQueryHandler
    : IRequestHandler<ListWorkflowInstanceHistoryQuery, Result<IReadOnlyList<WorkflowInstanceDto>>>
{
    private readonly IWorkflowInstanceRepository _repository;

    public ListWorkflowInstanceHistoryQueryHandler(IWorkflowInstanceRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<IReadOnlyList<WorkflowInstanceDto>>> Handle(
        ListWorkflowInstanceHistoryQuery request, CancellationToken cancellationToken)
    {
        var instances = await _repository.ListByDefinitionAsync(
            new WorkflowDefinitionId(request.WorkflowDefinitionId), request.TenantId, cancellationToken).ConfigureAwait(false);

        return Result.Success<IReadOnlyList<WorkflowInstanceDto>>(instances.Select(WorkflowEngineMapper.ToDto).ToList());
    }
}
