using Hris.Application.Abstractions;
using Hris.Modules.Attendance.Application;
using Hris.Modules.Attendance.Application.Dtos;
using Hris.Modules.Attendance.Application.Mapping;
using Hris.Modules.Attendance.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Attendance.Application.Queries;

/// <summary>
/// AttendanceAdjustment read surface (queries.md). Resolves against the aggregate repository this
/// sprint; the same tenant isolation, scope-before-pagination, and no-template rules apply as the
/// eventual projection would enforce.
/// </summary>

/// <summary>One adjustment, full detail including its approval chain.</summary>
public sealed record GetAttendanceAdjustmentQuery(Guid TenantId, Guid AttendanceAdjustmentId)
    : IQuery<Result<AttendanceAdjustmentDto>>;

internal sealed class GetAttendanceAdjustmentQueryHandler
    : IRequestHandler<GetAttendanceAdjustmentQuery, Result<AttendanceAdjustmentDto>>
{
    private readonly IAttendanceAdjustmentRepository _repository;

    public GetAttendanceAdjustmentQueryHandler(IAttendanceAdjustmentRepository repository) =>
        _repository = Guard.AgainstNull(repository, nameof(repository));

    public async Task<Result<AttendanceAdjustmentDto>> Handle(
        GetAttendanceAdjustmentQuery request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var result = await AttendanceLookup
            .LoadAdjustmentForTenantAsync(_repository, request.AttendanceAdjustmentId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        return result.IsFailure
            ? Result.Failure<AttendanceAdjustmentDto>(result.Error)
            : Result.Success(AttendanceMapper.ToDto(result.Value));
    }
}

/// <summary>
/// Adjustments awaiting the requesting user's review or approval. Pending means submitted but not yet
/// decided (AT-020 lifecycle). Pass <c>AttendanceRecordIds</c> to scope to a reviewer's configured
/// records, or null for the tenant-wide queue an HR administrator sees.
/// </summary>
public sealed record GetPendingAdjustmentsForReviewQuery(Guid TenantId, IReadOnlyList<Guid>? AttendanceRecordIds = null)
    : IQuery<Result<IReadOnlyList<AttendanceAdjustmentDto>>>;

internal sealed class GetPendingAdjustmentsForReviewQueryHandler
    : IRequestHandler<GetPendingAdjustmentsForReviewQuery, Result<IReadOnlyList<AttendanceAdjustmentDto>>>
{
    private readonly IAttendanceAdjustmentRepository _repository;

    public GetPendingAdjustmentsForReviewQueryHandler(IAttendanceAdjustmentRepository repository) =>
        _repository = Guard.AgainstNull(repository, nameof(repository));

    public async Task<Result<IReadOnlyList<AttendanceAdjustmentDto>>> Handle(
        GetPendingAdjustmentsForReviewQuery request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var adjustments = await _repository.ListByTenantAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        var pending = adjustments
            .Where(a => a.Status is AdjustmentStatus.Submitted or AdjustmentStatus.UnderReview &&
                        (request.AttendanceRecordIds is null || request.AttendanceRecordIds.Count == 0 ||
                         request.AttendanceRecordIds.Contains(a.AttendanceRecordId.Value)))
            .OrderBy(a => a.SubmittedOn)
            .Select(AttendanceMapper.ToDto)
            .ToList();

        return Result.Success((IReadOnlyList<AttendanceAdjustmentDto>)pending);
    }
}
