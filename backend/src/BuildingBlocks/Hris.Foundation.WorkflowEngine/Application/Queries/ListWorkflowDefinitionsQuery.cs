using Hris.Application.Abstractions;
using Hris.Foundation.WorkflowEngine.Application.Dtos;
using Hris.Foundation.WorkflowEngine.Application.Mapping;
using Hris.Foundation.WorkflowEngine.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.WorkflowEngine.Application.Queries;

public sealed record ListWorkflowDefinitionsQuery(Guid TenantId) : IQuery<Result<IReadOnlyList<WorkflowDefinitionDto>>>;

internal sealed class ListWorkflowDefinitionsQueryHandler
    : IRequestHandler<ListWorkflowDefinitionsQuery, Result<IReadOnlyList<WorkflowDefinitionDto>>>
{
    private readonly IWorkflowDefinitionRepository _repository;

    public ListWorkflowDefinitionsQueryHandler(IWorkflowDefinitionRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<IReadOnlyList<WorkflowDefinitionDto>>> Handle(
        ListWorkflowDefinitionsQuery request, CancellationToken cancellationToken)
    {
        var definitions = await _repository.ListByTenantAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        return Result.Success<IReadOnlyList<WorkflowDefinitionDto>>(definitions.Select(WorkflowEngineMapper.ToDto).ToList());
    }
}
