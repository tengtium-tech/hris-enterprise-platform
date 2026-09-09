using Hris.Application.Abstractions;
using Hris.Modules.Workflow.Application.Dtos;
using Hris.Modules.Workflow.Application.Mapping;
using Hris.Modules.Workflow.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Workflow.Application.Queries;

/// <summary>
/// Returns not-found for an identifier belonging to another tenant, never forbidden
/// (CTR-ISO-002).
/// </summary>
public sealed record GetWorkflowDefinitionByIdQuery(Guid DefinitionId, Guid TenantId) : IQuery<Result<WorkflowDefinitionDto>>;

internal sealed class GetWorkflowDefinitionByIdQueryHandler
    : IRequestHandler<GetWorkflowDefinitionByIdQuery, Result<WorkflowDefinitionDto>>
{
    private readonly IWorkflowDefinitionRepository _repository;

    public GetWorkflowDefinitionByIdQueryHandler(IWorkflowDefinitionRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<WorkflowDefinitionDto>> Handle(
        GetWorkflowDefinitionByIdQuery request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var result = await WorkflowLookup
            .LoadDefinitionForTenantAsync(_repository, request.DefinitionId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        return result.IsFailure
            ? Result.Failure<WorkflowDefinitionDto>(result.Error)
            : Result.Success(WorkflowMapper.ToDto(result.Value));
    }
}

public sealed record ListWorkflowDefinitionsQuery(Guid TenantId, Guid? BusinessProcessId, string? Status)
    : IQuery<Result<IReadOnlyList<WorkflowDefinitionDto>>>;

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
        Guard.AgainstNull(request, nameof(request));

        var definitions = await _repository.ListByTenantAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        var filtered = definitions
            .Where(definition => request.BusinessProcessId is null || definition.BusinessProcessId == request.BusinessProcessId)
            .Where(definition => request.Status is null
                || string.Equals(definition.Status.ToString(), request.Status, StringComparison.OrdinalIgnoreCase));

        IReadOnlyList<WorkflowDefinitionDto> dtos = filtered.Select(WorkflowMapper.ToDto).ToList();
        return Result.Success(dtos);
    }
}

/// <summary>
/// Every version in one lineage, current and superseded. This query exists because
/// workflow-versioning.md's guarantee that a running instance keeps its version is
/// only checkable if superseded versions remain queryable, not just the current one.
/// </summary>
public sealed record GetDefinitionVersionHistoryQuery(Guid LineageId, Guid TenantId)
    : IQuery<Result<IReadOnlyList<WorkflowDefinitionDto>>>;

internal sealed class GetDefinitionVersionHistoryQueryHandler
    : IRequestHandler<GetDefinitionVersionHistoryQuery, Result<IReadOnlyList<WorkflowDefinitionDto>>>
{
    private readonly IWorkflowDefinitionRepository _repository;

    public GetDefinitionVersionHistoryQueryHandler(IWorkflowDefinitionRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<IReadOnlyList<WorkflowDefinitionDto>>> Handle(
        GetDefinitionVersionHistoryQuery request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var versions = await _repository.ListByLineageAsync(request.LineageId, cancellationToken).ConfigureAwait(false);

        IReadOnlyList<WorkflowDefinitionDto> dtos = versions
            .Where(version => version.TenantId == request.TenantId)
            .Select(WorkflowMapper.ToDto)
            .ToList();

        return Result.Success(dtos);
    }
}
