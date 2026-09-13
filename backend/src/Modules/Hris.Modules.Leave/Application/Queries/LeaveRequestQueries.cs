using Hris.Application.Abstractions;
using Hris.Modules.Leave.Application.Dtos;
using Hris.Modules.Leave.Application.Mapping;
using Hris.Modules.Leave.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Leave.Application.Queries;

/// <summary>One request, full detail. Source: application/queries.md.</summary>
public sealed record GetLeaveRequestQuery(Guid TenantId, Guid LeaveRequestId) : IQuery<Result<LeaveRequestDto>>;

internal sealed class GetLeaveRequestQueryHandler : IRequestHandler<GetLeaveRequestQuery, Result<LeaveRequestDto>>
{
    private readonly ILeaveRequestRepository _repository;

    public GetLeaveRequestQueryHandler(ILeaveRequestRepository repository) =>
        _repository = Guard.AgainstNull(repository, nameof(repository));

    public async Task<Result<LeaveRequestDto>> Handle(GetLeaveRequestQuery request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var result = await LeaveLookup
            .LoadLeaveRequestForTenantAsync(_repository, request.LeaveRequestId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        return result.IsFailure
            ? Result.Failure<LeaveRequestDto>(result.Error)
            : Result.Success(LeaveMapper.ToDto(result.Value));
    }
}

/// <summary>Approved and pending leave for a set of direct reports. Source: application/queries.md.</summary>
public sealed record GetTeamLeaveCalendarQuery(Guid TenantId, IReadOnlyList<Guid> TeamEmployeeIds)
    : IQuery<Result<IReadOnlyList<LeaveRequestDto>>>;

internal sealed class GetTeamLeaveCalendarQueryHandler
    : IRequestHandler<GetTeamLeaveCalendarQuery, Result<IReadOnlyList<LeaveRequestDto>>>
{
    private readonly ILeaveRequestRepository _repository;

    public GetTeamLeaveCalendarQueryHandler(ILeaveRequestRepository repository) =>
        _repository = Guard.AgainstNull(repository, nameof(repository));

    public async Task<Result<IReadOnlyList<LeaveRequestDto>>> Handle(
        GetTeamLeaveCalendarQuery request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var requests = await _repository.ListByTenantAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        var calendar = requests
            .Where(r => request.TeamEmployeeIds.Contains(r.EmployeeId)
                        && r.Status is LeaveRequestStatus.PendingApproval or LeaveRequestStatus.Approved)
            .OrderBy(r => r.DateRange.StartDate)
            .Select(LeaveMapper.ToDto)
            .ToList();

        return Result.Success((IReadOnlyList<LeaveRequestDto>)calendar);
    }
}

/// <summary>Requests awaiting the requesting user's decision. Source: application/queries.md.</summary>
public sealed record GetPendingApprovalsQuery(Guid TenantId, IReadOnlyList<Guid> TeamEmployeeIds)
    : IQuery<Result<IReadOnlyList<LeaveRequestDto>>>;

internal sealed class GetPendingApprovalsQueryHandler
    : IRequestHandler<GetPendingApprovalsQuery, Result<IReadOnlyList<LeaveRequestDto>>>
{
    private readonly ILeaveRequestRepository _repository;

    public GetPendingApprovalsQueryHandler(ILeaveRequestRepository repository) =>
        _repository = Guard.AgainstNull(repository, nameof(repository));

    public async Task<Result<IReadOnlyList<LeaveRequestDto>>> Handle(
        GetPendingApprovalsQuery request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var requests = await _repository.ListByTenantAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        var pending = requests
            .Where(r => r.Status == LeaveRequestStatus.PendingApproval && request.TeamEmployeeIds.Contains(r.EmployeeId))
            .OrderBy(r => r.SubmittedOn)
            .Select(LeaveMapper.ToDto)
            .ToList();

        return Result.Success((IReadOnlyList<LeaveRequestDto>)pending);
    }
}

/// <summary>
/// Every approved leave and LWOP period for a payroll period and scope — complete, stable,
/// carries <see cref="Domain.PayTreatment"/> directly. The query <c>payroll</c> will depend
/// on. Source: application/queries.md.
/// </summary>
public sealed record GetApprovedLeaveForPayrollPeriodQuery(Guid TenantId, DateOnly PeriodStart, DateOnly PeriodEnd)
    : IQuery<Result<IReadOnlyList<LeaveRequestDto>>>;

internal sealed class GetApprovedLeaveForPayrollPeriodQueryHandler
    : IRequestHandler<GetApprovedLeaveForPayrollPeriodQuery, Result<IReadOnlyList<LeaveRequestDto>>>
{
    private readonly ILeaveRequestRepository _repository;

    public GetApprovedLeaveForPayrollPeriodQueryHandler(ILeaveRequestRepository repository) =>
        _repository = Guard.AgainstNull(repository, nameof(repository));

    public async Task<Result<IReadOnlyList<LeaveRequestDto>>> Handle(
        GetApprovedLeaveForPayrollPeriodQuery request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var requests = await _repository.ListByTenantAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        var approved = requests
            .Where(r => r.Status == LeaveRequestStatus.Approved
                        && r.DateRange.StartDate <= request.PeriodEnd && request.PeriodStart <= r.DateRange.EndDate)
            .OrderBy(r => r.EmployeeId)
            .ThenBy(r => r.DateRange.StartDate)
            .Select(LeaveMapper.ToDto)
            .ToList();

        return Result.Success((IReadOnlyList<LeaveRequestDto>)approved);
    }
}

/// <summary>Approved leave starting within a configurable window. Source: application/queries.md.</summary>
public sealed record GetUpcomingLeaveQuery(Guid TenantId, DateOnly FromDate, DateOnly ToDate, IReadOnlyList<Guid>? EmployeeIds = null)
    : IQuery<Result<IReadOnlyList<LeaveRequestDto>>>;

internal sealed class GetUpcomingLeaveQueryHandler
    : IRequestHandler<GetUpcomingLeaveQuery, Result<IReadOnlyList<LeaveRequestDto>>>
{
    private readonly ILeaveRequestRepository _repository;

    public GetUpcomingLeaveQueryHandler(ILeaveRequestRepository repository) =>
        _repository = Guard.AgainstNull(repository, nameof(repository));

    public async Task<Result<IReadOnlyList<LeaveRequestDto>>> Handle(GetUpcomingLeaveQuery request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var requests = await _repository.ListByTenantAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        var upcoming = requests
            .Where(r => r.Status == LeaveRequestStatus.Approved
                        && r.DateRange.StartDate >= request.FromDate && r.DateRange.StartDate <= request.ToDate
                        && (request.EmployeeIds is null || request.EmployeeIds.Count == 0 || request.EmployeeIds.Contains(r.EmployeeId)))
            .OrderBy(r => r.DateRange.StartDate)
            .Select(LeaveMapper.ToDto)
            .ToList();

        return Result.Success((IReadOnlyList<LeaveRequestDto>)upcoming);
    }
}
