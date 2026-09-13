using Hris.Application.Abstractions;
using Hris.Modules.Attendance.Application;
using Hris.Modules.Attendance.Application.Dtos;
using Hris.Modules.Attendance.Application.Mapping;
using Hris.Modules.Attendance.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Attendance.Application.Queries;

/// <summary>
/// OvertimeRequest read surface (queries.md). Resolves against the aggregate repository this sprint;
/// tenant isolation and scope-before-pagination follow the module's standard (query-handlers.md).
/// </summary>

/// <summary>One overtime request, full detail.</summary>
public sealed record GetOvertimeRequestQuery(Guid TenantId, Guid OvertimeRequestId)
    : IQuery<Result<OvertimeRequestDto>>;

internal sealed class GetOvertimeRequestQueryHandler
    : IRequestHandler<GetOvertimeRequestQuery, Result<OvertimeRequestDto>>
{
    private readonly IOvertimeRequestRepository _repository;

    public GetOvertimeRequestQueryHandler(IOvertimeRequestRepository repository) =>
        _repository = Guard.AgainstNull(repository, nameof(repository));

    public async Task<Result<OvertimeRequestDto>> Handle(
        GetOvertimeRequestQuery request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var result = await AttendanceLookup
            .LoadOvertimeRequestForTenantAsync(_repository, request.OvertimeRequestId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        return result.IsFailure
            ? Result.Failure<OvertimeRequestDto>(result.Error)
            : Result.Success(AttendanceMapper.ToDto(result.Value));
    }
}

/// <summary>
/// Overtime requests awaiting approval. "Awaiting" means not yet decided — anything that is not
/// Approved, Rejected, or Cancelled (AT-040, AT-041). Scoped to the supplied team; an empty set
/// yields nothing.
/// </summary>
public sealed record GetPendingOvertimeRequestsQuery(Guid TenantId, IReadOnlyList<Guid> TeamEmployeeIds)
    : IQuery<Result<IReadOnlyList<OvertimeRequestDto>>>;

internal sealed class GetPendingOvertimeRequestsQueryHandler
    : IRequestHandler<GetPendingOvertimeRequestsQuery, Result<IReadOnlyList<OvertimeRequestDto>>>
{
    private readonly IOvertimeRequestRepository _repository;

    public GetPendingOvertimeRequestsQueryHandler(IOvertimeRequestRepository repository) =>
        _repository = Guard.AgainstNull(repository, nameof(repository));

    public async Task<Result<IReadOnlyList<OvertimeRequestDto>>> Handle(
        GetPendingOvertimeRequestsQuery request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var requests = await _repository.ListByTenantAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        var pending = requests
            .Where(r => r.Status is not (ApprovalStatus.Approved or ApprovalStatus.Rejected or ApprovalStatus.Cancelled) &&
                        request.TeamEmployeeIds.Contains(r.EmployeeId))
            .OrderBy(r => r.EmployeeId)
            .ThenBy(r => r.WorkDate)
            .Select(AttendanceMapper.ToDto)
            .ToList();

        return Result.Success((IReadOnlyList<OvertimeRequestDto>)pending);
    }
}
