using Hris.Application.Abstractions;
using Hris.Modules.Workflow.Application.Dtos;
using Hris.Modules.Workflow.Application.Mapping;
using Hris.Modules.Workflow.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Workflow.Application.Queries;

public sealed record GetApprovalDelegationQuery(Guid DelegationId, Guid TenantId) : IQuery<Result<ApprovalDelegationDto>>;

internal sealed class GetApprovalDelegationQueryHandler
    : IRequestHandler<GetApprovalDelegationQuery, Result<ApprovalDelegationDto>>
{
    private readonly IApprovalDelegationRepository _repository;

    public GetApprovalDelegationQueryHandler(IApprovalDelegationRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<ApprovalDelegationDto>> Handle(
        GetApprovalDelegationQuery request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var result = await WorkflowLookup
            .LoadDelegationForTenantAsync(_repository, request.DelegationId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        return result.IsFailure
            ? Result.Failure<ApprovalDelegationDto>(result.Error)
            : Result.Success(WorkflowMapper.ToDto(result.Value));
    }
}

public sealed record ListApprovalDelegationsQuery(Guid TenantId, Guid? DelegatorUserAccountId)
    : IQuery<Result<IReadOnlyList<ApprovalDelegationDto>>>;

internal sealed class ListApprovalDelegationsQueryHandler
    : IRequestHandler<ListApprovalDelegationsQuery, Result<IReadOnlyList<ApprovalDelegationDto>>>
{
    private readonly IApprovalDelegationRepository _repository;

    public ListApprovalDelegationsQueryHandler(IApprovalDelegationRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<IReadOnlyList<ApprovalDelegationDto>>> Handle(
        ListApprovalDelegationsQuery request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var delegations = request.DelegatorUserAccountId is null
            ? await _repository.ListByTenantAsync(request.TenantId, cancellationToken).ConfigureAwait(false)
            : await _repository.ListByDelegatorAsync(
                request.TenantId, request.DelegatorUserAccountId.Value, cancellationToken).ConfigureAwait(false);

        IReadOnlyList<ApprovalDelegationDto> dtos = delegations.Select(WorkflowMapper.ToDto).ToList();
        return Result.Success(dtos);
    }
}
