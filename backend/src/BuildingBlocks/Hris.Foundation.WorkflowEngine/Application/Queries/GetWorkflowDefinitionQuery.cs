using Hris.Application.Abstractions;
using Hris.Foundation.WorkflowEngine.Application.Dtos;
using Hris.Foundation.WorkflowEngine.Application.Mapping;
using Hris.Foundation.WorkflowEngine.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.WorkflowEngine.Application.Queries;

public sealed record GetWorkflowDefinitionQuery(Guid WorkflowDefinitionId) : IQuery<Result<WorkflowDefinitionDto>>;

internal sealed class GetWorkflowDefinitionQueryHandler : IRequestHandler<GetWorkflowDefinitionQuery, Result<WorkflowDefinitionDto>>
{
    private readonly IWorkflowDefinitionRepository _repository;

    public GetWorkflowDefinitionQueryHandler(IWorkflowDefinitionRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<WorkflowDefinitionDto>> Handle(GetWorkflowDefinitionQuery request, CancellationToken cancellationToken)
    {
        var definition = await _repository.GetByIdAsync(
            new WorkflowDefinitionId(request.WorkflowDefinitionId), cancellationToken).ConfigureAwait(false);

        return definition is null
            ? Result.Failure<WorkflowDefinitionDto>(WorkflowEngineErrors.DefinitionNotFound)
            : Result.Success(WorkflowEngineMapper.ToDto(definition));
    }
}
