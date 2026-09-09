using Hris.Application.Abstractions;
using Hris.Modules.Workflow.Application.Dtos;
using Hris.Modules.Workflow.Application.Mapping;
using Hris.Modules.Workflow.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Workflow.Application.Queries;

/// <summary>
/// A sensitive query, per queries.md's own table: a tenant's approval configuration
/// reveals which controls are in place and which are not. Auditing the read is the
/// security layer's concern, not this handler's, but the classification is recorded
/// here so it is not lost.
/// </summary>
public sealed record GetApprovalPolicyQuery(Guid TenantId) : IQuery<Result<ApprovalPolicyDto>>;

internal sealed class GetApprovalPolicyQueryHandler : IRequestHandler<GetApprovalPolicyQuery, Result<ApprovalPolicyDto>>
{
    private readonly IApprovalPolicyRepository _repository;

    public GetApprovalPolicyQueryHandler(IApprovalPolicyRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<ApprovalPolicyDto>> Handle(GetApprovalPolicyQuery request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var result = await WorkflowLookup
            .LoadPolicyForTenantAsync(_repository, request.TenantId, cancellationToken).ConfigureAwait(false);

        return result.IsFailure
            ? Result.Failure<ApprovalPolicyDto>(result.Error)
            : Result.Success(WorkflowMapper.ToDto(result.Value));
    }
}
