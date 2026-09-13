using Hris.Application.Abstractions;
using Hris.Modules.Leave.Application.Dtos;
using Hris.Modules.Leave.Application.Mapping;
using Hris.Modules.Leave.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Leave.Application.Queries;

/// <summary>
/// The policy version effective for a leave type on a given date (LV-011). The caller
/// assembles <c>CandidateTargetIds</c> — the employee identifier plus every organizational
/// unit they sit in, most specific first — and the handler selects the first matching
/// assignment, exactly as Attendance's own <c>GetEffectiveAttendancePolicyQuery</c> does.
/// Source: application/queries.md.
/// </summary>
public sealed record GetEffectiveLeavePolicyQuery(
    Guid TenantId, Guid LeaveTypeId, IReadOnlyList<string> CandidateTargetIds, DateOnly AsOfDate)
    : IQuery<Result<LeavePolicyDto>>;

internal sealed class GetEffectiveLeavePolicyQueryHandler
    : IRequestHandler<GetEffectiveLeavePolicyQuery, Result<LeavePolicyDto>>
{
    private readonly ILeavePolicyRepository _repository;

    public GetEffectiveLeavePolicyQueryHandler(ILeavePolicyRepository repository) =>
        _repository = Guard.AgainstNull(repository, nameof(repository));

    public async Task<Result<LeavePolicyDto>> Handle(GetEffectiveLeavePolicyQuery request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var candidates = await _repository
            .ListEffectiveForLeaveTypeAsync(request.TenantId, request.LeaveTypeId, request.AsOfDate, cancellationToken)
            .ConfigureAwait(false);

        var effective = candidates.FirstOrDefault(policy => policy.PolicyAssignments.Any(assignment =>
            request.CandidateTargetIds.Contains(assignment.ScopeTargetId, StringComparer.Ordinal) &&
            assignment.IsEffectiveOn(request.AsOfDate)));

        return effective is null
            ? Result.Failure<LeavePolicyDto>(LeaveErrors.LeavePolicyNotFound)
            : Result.Success(LeaveMapper.ToDto(effective));
    }
}

/// <summary>Every version of a policy lineage, with effective periods. Source: application/queries.md.</summary>
public sealed record GetLeavePolicyHistoryQuery(Guid TenantId, Guid LineageId) : IQuery<Result<IReadOnlyList<LeavePolicyDto>>>;

internal sealed class GetLeavePolicyHistoryQueryHandler
    : IRequestHandler<GetLeavePolicyHistoryQuery, Result<IReadOnlyList<LeavePolicyDto>>>
{
    private readonly ILeavePolicyRepository _repository;

    public GetLeavePolicyHistoryQueryHandler(ILeavePolicyRepository repository) =>
        _repository = Guard.AgainstNull(repository, nameof(repository));

    public async Task<Result<IReadOnlyList<LeavePolicyDto>>> Handle(
        GetLeavePolicyHistoryQuery request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var versions = await _repository.ListByLineageAsync(request.LineageId, cancellationToken).ConfigureAwait(false);

        var history = versions
            .Where(policy => policy.TenantId == request.TenantId)
            .OrderBy(policy => policy.Version)
            .Select(LeaveMapper.ToDto)
            .ToList();

        return Result.Success((IReadOnlyList<LeavePolicyDto>)history);
    }
}
