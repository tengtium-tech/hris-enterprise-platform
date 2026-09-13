using Hris.Application.Abstractions;
using Hris.Modules.Leave.Application.Dtos;
using Hris.Modules.Leave.Application.Mapping;
using Hris.Modules.Leave.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Leave.Application.Queries;

/// <summary>One encashment request, full detail. Source: application/queries.md.</summary>
public sealed record GetLeaveEncashmentQuery(Guid TenantId, Guid LeaveEncashmentId) : IQuery<Result<LeaveEncashmentDto>>;

internal sealed class GetLeaveEncashmentQueryHandler : IRequestHandler<GetLeaveEncashmentQuery, Result<LeaveEncashmentDto>>
{
    private readonly ILeaveEncashmentRepository _repository;

    public GetLeaveEncashmentQueryHandler(ILeaveEncashmentRepository repository) =>
        _repository = Guard.AgainstNull(repository, nameof(repository));

    public async Task<Result<LeaveEncashmentDto>> Handle(GetLeaveEncashmentQuery request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var result = await LeaveLookup
            .LoadLeaveEncashmentForTenantAsync(_repository, request.LeaveEncashmentId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        return result.IsFailure
            ? Result.Failure<LeaveEncashmentDto>(result.Error)
            : Result.Success(LeaveMapper.ToDto(result.Value));
    }
}

/// <summary>Encashment requests awaiting the requesting <c>HRManager</c>'s approval. Source: application/queries.md.</summary>
public sealed record GetPendingEncashmentsForApprovalQuery(Guid TenantId) : IQuery<Result<IReadOnlyList<LeaveEncashmentDto>>>;

internal sealed class GetPendingEncashmentsForApprovalQueryHandler
    : IRequestHandler<GetPendingEncashmentsForApprovalQuery, Result<IReadOnlyList<LeaveEncashmentDto>>>
{
    private readonly ILeaveEncashmentRepository _repository;

    public GetPendingEncashmentsForApprovalQueryHandler(ILeaveEncashmentRepository repository) =>
        _repository = Guard.AgainstNull(repository, nameof(repository));

    public async Task<Result<IReadOnlyList<LeaveEncashmentDto>>> Handle(
        GetPendingEncashmentsForApprovalQuery request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var encashments = await _repository.ListByTenantAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        var pending = encashments
            .Where(e => e.Status == LeaveEncashmentStatus.PendingApproval)
            .OrderBy(e => e.SubmittedOn)
            .Select(LeaveMapper.ToDto)
            .ToList();

        return Result.Success((IReadOnlyList<LeaveEncashmentDto>)pending);
    }
}
