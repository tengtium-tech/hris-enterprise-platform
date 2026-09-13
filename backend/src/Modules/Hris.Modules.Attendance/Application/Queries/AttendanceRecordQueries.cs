using Hris.Application.Abstractions;
using Hris.Modules.Attendance.Application;
using Hris.Modules.Attendance.Application.Dtos;
using Hris.Modules.Attendance.Application.Mapping;
using Hris.Modules.Attendance.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Attendance.Application.Queries;

/// <summary>
/// AttendanceRecord read surface. Per queries.md these read from a projection; until that
/// read-model exists this sprint the handlers resolve against the aggregate repository and apply
/// the same tenant isolation and scope-before-pagination discipline the projection would enforce
/// (query-handlers.md, CTR-AUT-009). Field-security filtering is likewise applied at the projection
/// layer once built — today the caller's scope is enforced by the tenant-isolated load and the
/// caller-supplied employee set.
/// </summary>

/// <summary>One record, full detail (queries.md). Tenant isolation via the shared lookup.</summary>
public sealed record GetAttendanceRecordQuery(Guid TenantId, Guid AttendanceRecordId)
    : IQuery<Result<AttendanceRecordDto>>;

internal sealed class GetAttendanceRecordQueryHandler
    : IRequestHandler<GetAttendanceRecordQuery, Result<AttendanceRecordDto>>
{
    private readonly IAttendanceRecordRepository _repository;

    public GetAttendanceRecordQueryHandler(IAttendanceRecordRepository repository) =>
        _repository = Guard.AgainstNull(repository, nameof(repository));

    public async Task<Result<AttendanceRecordDto>> Handle(
        GetAttendanceRecordQuery request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var result = await AttendanceLookup
            .LoadRecordForTenantAsync(_repository, request.AttendanceRecordId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        return result.IsFailure
            ? Result.Failure<AttendanceRecordDto>(result.Error)
            : Result.Success(AttendanceMapper.ToDto(result.Value));
    }
}

/// <summary>Records for one employee across a date range, ordered by work date.</summary>
public sealed record GetAttendanceForPeriodQuery(Guid TenantId, Guid EmployeeId, DateOnly From, DateOnly To)
    : IQuery<Result<IReadOnlyList<AttendanceRecordSummaryDto>>>;

internal sealed class GetAttendanceForPeriodQueryHandler
    : IRequestHandler<GetAttendanceForPeriodQuery, Result<IReadOnlyList<AttendanceRecordSummaryDto>>>
{
    private readonly IAttendanceRecordRepository _repository;

    public GetAttendanceForPeriodQueryHandler(IAttendanceRecordRepository repository) =>
        _repository = Guard.AgainstNull(repository, nameof(repository));

    public async Task<Result<IReadOnlyList<AttendanceRecordSummaryDto>>> Handle(
        GetAttendanceForPeriodQuery request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var records = await _repository.ListByTenantAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        var matches = records
            .Where(r => r.EmployeeId == request.EmployeeId && r.WorkDate >= request.From && r.WorkDate <= request.To)
            .OrderBy(r => r.WorkDate)
            .Select(AttendanceMapper.ToSummary)
            .ToList();

        return Result.Success((IReadOnlyList<AttendanceRecordSummaryDto>)matches);
    }
}

/// <summary>
/// Records pending the requesting PeopleManager's approval. The caller supplies the employee set
/// that forms their reporting line; we filter server-side rather than reading broadly and trimming
/// client-side (query-handlers.md). An empty set yields no records — a manager with no reports sees
/// nothing, which is the safe default.
/// </summary>
public sealed record GetTeamAttendanceForApprovalQuery(Guid TenantId, IReadOnlyList<Guid> TeamEmployeeIds)
    : IQuery<Result<IReadOnlyList<AttendanceRecordSummaryDto>>>;

internal sealed class GetTeamAttendanceForApprovalQueryHandler
    : IRequestHandler<GetTeamAttendanceForApprovalQuery, Result<IReadOnlyList<AttendanceRecordSummaryDto>>>
{
    private readonly IAttendanceRecordRepository _repository;

    public GetTeamAttendanceForApprovalQueryHandler(IAttendanceRecordRepository repository) =>
        _repository = Guard.AgainstNull(repository, nameof(repository));

    public async Task<Result<IReadOnlyList<AttendanceRecordSummaryDto>>> Handle(
        GetTeamAttendanceForApprovalQuery request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var records = await _repository.ListByTenantAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        var matches = records
            .Where(r => r.ApprovalStatus == ApprovalStatus.Pending && request.TeamEmployeeIds.Contains(r.EmployeeId))
            .OrderBy(r => r.EmployeeId)
            .ThenBy(r => r.WorkDate)
            .Select(AttendanceMapper.ToSummary)
            .ToList();

        return Result.Success((IReadOnlyList<AttendanceRecordSummaryDto>)matches);
    }
}

/// <summary>
/// Every finalized record for a payroll period and scope, complete and stable (queries.md). Excludes
/// any record still carrying a pending adjustment, and orders deterministically by employee then work
/// date so two calls against unchanged data return byte-identical results (query-handlers.md, AT-003).
/// </summary>
public sealed record GetFinalizedAttendanceForPayrollPeriodQuery(Guid TenantId, DateOnly From, DateOnly To)
    : IQuery<Result<IReadOnlyList<AttendanceRecordSummaryDto>>>;

internal sealed class GetFinalizedAttendanceForPayrollPeriodQueryHandler
    : IRequestHandler<GetFinalizedAttendanceForPayrollPeriodQuery, Result<IReadOnlyList<AttendanceRecordSummaryDto>>>
{
    private readonly IAttendanceRecordRepository _repository;

    public GetFinalizedAttendanceForPayrollPeriodQueryHandler(IAttendanceRecordRepository repository) =>
        _repository = Guard.AgainstNull(repository, nameof(repository));

    public async Task<Result<IReadOnlyList<AttendanceRecordSummaryDto>>> Handle(
        GetFinalizedAttendanceForPayrollPeriodQuery request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var finalized = await _repository
            .GetFinalizedForPayrollPeriodAsync(request.TenantId, request.From, request.To, cancellationToken)
            .ConfigureAwait(false);

        var matches = finalized
            .Where(r => r.PendingAdjustmentCount == 0)
            .OrderBy(r => r.EmployeeId)
            .ThenBy(r => r.WorkDate)
            .Select(AttendanceMapper.ToSummary)
            .ToList();

        return Result.Success((IReadOnlyList<AttendanceRecordSummaryDto>)matches);
    }
}

/// <summary>
/// Records carrying unresolved validation or calculation exceptions. Returns the full record so the
/// caller can see the exception text; pass <c>EmployeeIds</c> to scope to a team, or null for the
/// tenant-wide view an HR administrator holds (queries.md scope note).
/// </summary>
public sealed record GetAttendanceExceptionsQuery(Guid TenantId, IReadOnlyList<Guid>? EmployeeIds = null)
    : IQuery<Result<IReadOnlyList<AttendanceRecordDto>>>;

internal sealed class GetAttendanceExceptionsQueryHandler
    : IRequestHandler<GetAttendanceExceptionsQuery, Result<IReadOnlyList<AttendanceRecordDto>>>
{
    private readonly IAttendanceRecordRepository _repository;

    public GetAttendanceExceptionsQueryHandler(IAttendanceRecordRepository repository) =>
        _repository = Guard.AgainstNull(repository, nameof(repository));

    public async Task<Result<IReadOnlyList<AttendanceRecordDto>>> Handle(
        GetAttendanceExceptionsQuery request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var records = await _repository.ListByTenantAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        var matches = records
            .Where(r => r.Exceptions.Count > 0 &&
                        (request.EmployeeIds is null || request.EmployeeIds.Count == 0 ||
                         request.EmployeeIds.Contains(r.EmployeeId)))
            .OrderBy(r => r.EmployeeId)
            .ThenBy(r => r.WorkDate)
            .Select(AttendanceMapper.ToDto)
            .ToList();

        return Result.Success((IReadOnlyList<AttendanceRecordDto>)matches);
    }
}
