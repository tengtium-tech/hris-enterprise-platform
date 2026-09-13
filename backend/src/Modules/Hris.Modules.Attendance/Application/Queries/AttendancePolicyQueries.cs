using Hris.Application.Abstractions;
using Hris.Modules.Attendance.Application.Dtos;
using Hris.Modules.Attendance.Application.Mapping;
using Hris.Modules.Attendance.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Attendance.Application.Queries;

/// <summary>
/// AttendancePolicy read surface (queries.md). This sprint resolves the effective version from the
/// aggregate store; the point-in-time guarantee (AT-002, CTR-DAT-005) is preserved because the
/// repository returns only versions effective on the date and the selection below re-checks each
/// assignment's own effective window.
/// </summary>

/// <summary>
/// The policy version effective for an employee on a given date (AT-002). The caller assembles
/// <c>CandidateTargetIds</c> — the employee identifier plus every organizational unit they sit in —
/// and the handler selects the effective match, exactly as <c>RunCalculationCommandHandler</c> does,
/// so the query and the calculation engine never disagree about which policy governs a date.
/// </summary>
public sealed record GetEffectiveAttendancePolicyQuery(
    Guid TenantId, IReadOnlyList<string> CandidateTargetIds, DateOnly AsOfDate)
    : IQuery<Result<AttendancePolicyDto>>;

internal sealed class GetEffectiveAttendancePolicyQueryHandler
    : IRequestHandler<GetEffectiveAttendancePolicyQuery, Result<AttendancePolicyDto>>
{
    private readonly IAttendancePolicyRepository _repository;

    public GetEffectiveAttendancePolicyQueryHandler(IAttendancePolicyRepository repository) =>
        _repository = Guard.AgainstNull(repository, nameof(repository));

    public async Task<Result<AttendancePolicyDto>> Handle(
        GetEffectiveAttendancePolicyQuery request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var candidates = await _repository
            .ListEffectiveForWorkDateAsync(request.TenantId, request.AsOfDate, cancellationToken)
            .ConfigureAwait(false);

        var effective = candidates.FirstOrDefault(policy => policy.PolicyAssignments.Any(assignment =>
            request.CandidateTargetIds.Contains(assignment.ScopeTargetId, StringComparer.Ordinal) &&
            assignment.IsEffectiveOn(request.AsOfDate)));

        return effective is null
            ? Result.Failure<AttendancePolicyDto>(AttendanceErrors.AttendancePolicyNotFound)
            : Result.Success(AttendanceMapper.ToDto(effective));
    }
}
