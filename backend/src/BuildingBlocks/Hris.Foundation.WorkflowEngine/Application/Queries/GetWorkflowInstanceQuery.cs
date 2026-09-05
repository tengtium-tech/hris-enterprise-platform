using Hris.Application.Abstractions;
using Hris.Foundation.WorkflowEngine.Application.Dtos;
using Hris.Foundation.WorkflowEngine.Application.Mapping;
using Hris.Foundation.WorkflowEngine.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.WorkflowEngine.Application.Queries;

public sealed record GetWorkflowInstanceQuery(Guid WorkflowInstanceId) : IQuery<Result<WorkflowInstanceDto>>;

internal sealed class GetWorkflowInstanceQueryHandler : IRequestHandler<GetWorkflowInstanceQuery, Result<WorkflowInstanceDto>>
{
    private readonly IWorkflowInstanceRepository _repository;

    public GetWorkflowInstanceQueryHandler(IWorkflowInstanceRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<WorkflowInstanceDto>> Handle(GetWorkflowInstanceQuery request, CancellationToken cancellationToken)
    {
        var instance = await _repository.GetByIdAsync(
            new WorkflowInstanceId(request.WorkflowInstanceId), cancellationToken).ConfigureAwait(false);

        return instance is null
            ? Result.Failure<WorkflowInstanceDto>(WorkflowEngineErrors.InstanceNotFound)
            : Result.Success(WorkflowEngineMapper.ToDto(instance));
    }
}
