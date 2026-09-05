using Hris.Application.Abstractions;
using Hris.Foundation.WorkflowEngine.Application.Dtos;
using Hris.Foundation.WorkflowEngine.Application.Mapping;
using Hris.Foundation.WorkflowEngine.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.WorkflowEngine.Application.Queries;

public sealed record GetWorkflowTaskQuery(Guid WorkflowTaskId) : IQuery<Result<WorkflowTaskDto>>;

internal sealed class GetWorkflowTaskQueryHandler : IRequestHandler<GetWorkflowTaskQuery, Result<WorkflowTaskDto>>
{
    private readonly IWorkflowTaskRepository _repository;

    public GetWorkflowTaskQueryHandler(IWorkflowTaskRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<WorkflowTaskDto>> Handle(GetWorkflowTaskQuery request, CancellationToken cancellationToken)
    {
        var task = await _repository.GetByIdAsync(new WorkflowTaskId(request.WorkflowTaskId), cancellationToken).ConfigureAwait(false);

        return task is null
            ? Result.Failure<WorkflowTaskDto>(WorkflowEngineErrors.TaskNotFound)
            : Result.Success(WorkflowEngineMapper.ToDto(task));
    }
}
