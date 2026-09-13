using Hris.Application.Abstractions;
using Hris.Modules.Leave.Application.Dtos;
using Hris.Modules.Leave.Application.Mapping;
using Hris.Modules.Leave.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Leave.Application.Queries;

/// <summary>One adjustment, full detail. Source: application/queries.md.</summary>
public sealed record GetLeaveAdjustmentQuery(Guid TenantId, Guid LeaveAdjustmentId) : IQuery<Result<LeaveAdjustmentDto>>;

internal sealed class GetLeaveAdjustmentQueryHandler : IRequestHandler<GetLeaveAdjustmentQuery, Result<LeaveAdjustmentDto>>
{
    private readonly ILeaveAdjustmentRepository _repository;

    public GetLeaveAdjustmentQueryHandler(ILeaveAdjustmentRepository repository) =>
        _repository = Guard.AgainstNull(repository, nameof(repository));

    public async Task<Result<LeaveAdjustmentDto>> Handle(GetLeaveAdjustmentQuery request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var result = await LeaveLookup
            .LoadLeaveAdjustmentForTenantAsync(_repository, request.LeaveAdjustmentId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        return result.IsFailure
            ? Result.Failure<LeaveAdjustmentDto>(result.Error)
            : Result.Success(LeaveMapper.ToDto(result.Value));
    }
}

/// <summary>
/// Adjustments awaiting the requesting <c>HRManager</c>/authorized <c>HROfficer</c>'s review or
/// approval. Source: application/queries.md.
/// </summary>
public sealed record GetPendingAdjustmentsForReviewQuery(Guid TenantId) : IQuery<Result<IReadOnlyList<LeaveAdjustmentDto>>>;

internal sealed class GetPendingAdjustmentsForReviewQueryHandler
    : IRequestHandler<GetPendingAdjustmentsForReviewQuery, Result<IReadOnlyList<LeaveAdjustmentDto>>>
{
    private readonly ILeaveAdjustmentRepository _repository;

    public GetPendingAdjustmentsForReviewQueryHandler(ILeaveAdjustmentRepository repository) =>
        _repository = Guard.AgainstNull(repository, nameof(repository));

    public async Task<Result<IReadOnlyList<LeaveAdjustmentDto>>> Handle(
        GetPendingAdjustmentsForReviewQuery request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var adjustments = await _repository.ListByTenantAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        var pending = adjustments
            .Where(a => a.Status is LeaveAdjustmentStatus.Submitted or LeaveAdjustmentStatus.UnderReview)
            .OrderBy(a => a.SubmittedOn)
            .Select(LeaveMapper.ToDto)
            .ToList();

        return Result.Success((IReadOnlyList<LeaveAdjustmentDto>)pending);
    }
}
