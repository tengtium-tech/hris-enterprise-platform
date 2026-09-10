using Hris.Application.Abstractions;
using Hris.Modules.Timekeeping.Application.Dtos;
using Hris.Modules.Timekeeping.Application.Mapping;
using Hris.Modules.Timekeeping.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Timekeeping.Application.Queries;

/// <summary>
/// The query this whole module exists to answer, and the one <c>attendance</c> will
/// call for every employee on every processed date. Source:
/// docs/04-modules/timekeeping/application/queries.md.
///
/// <paramref name="CandidateTargetIds"/> carries the employee's own identifier plus
/// the identifiers of every organizational unit they sit in — position, department,
/// business unit, legal entity, company. Those come from <c>organization</c> and
/// <c>employment</c>, which this module references by identifier and never joins to,
/// so the caller assembles the list and this handler decides. That split is what
/// keeps <see cref="ShiftAssignmentResolver"/> a pure, exhaustively testable
/// function.
/// </summary>
public sealed record ResolveShiftForEmployeeOnDateQuery(
    Guid TenantId, IReadOnlyList<string> CandidateTargetIds, DateOnly WorkDate) : IQuery<Result<ShiftResolutionDto>>;

internal sealed class ResolveShiftForEmployeeOnDateQueryHandler
    : IRequestHandler<ResolveShiftForEmployeeOnDateQuery, Result<ShiftResolutionDto>>
{
    private readonly IShiftAssignmentRepository _repository;

    public ResolveShiftForEmployeeOnDateQueryHandler(IShiftAssignmentRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<ShiftResolutionDto>> Handle(
        ResolveShiftForEmployeeOnDateQuery request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var candidates = new List<ShiftAssignment>();
        foreach (var targetId in request.CandidateTargetIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.Ordinal))
        {
            var found = await _repository.ListByTargetAsync(request.TenantId, targetId.Trim(), cancellationToken)
                .ConfigureAwait(false);
            candidates.AddRange(found);
        }

        var resolution = ShiftAssignmentResolver.Resolve(candidates, request.WorkDate);
        return Result.Success(TimekeepingMapper.ToDto(resolution));
    }
}

/// <summary>
/// Resolves whether a date is a holiday for a scope, across every layer, using each
/// layer's version in force on that date (TK-002, TK-042).
///
/// The version filtering happens inside <see cref="HolidayResolver"/> rather than in
/// the repository query, deliberately: it means a caller cannot get TK-002 wrong by
/// passing the full history, because passing the full history is the expected input.
/// </summary>
public sealed record ResolveHolidayForScopeOnDateQuery(Guid TenantId, Guid LeafCalendarId, DateOnly Date)
    : IQuery<Result<HolidayResolutionDto>>;

internal sealed class ResolveHolidayForScopeOnDateQueryHandler
    : IRequestHandler<ResolveHolidayForScopeOnDateQuery, Result<HolidayResolutionDto>>
{
    private readonly IHolidayCalendarRepository _repository;

    public ResolveHolidayForScopeOnDateQueryHandler(IHolidayCalendarRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<HolidayResolutionDto>> Handle(
        ResolveHolidayForScopeOnDateQuery request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var leafResult = await TimekeepingLookup
            .LoadCalendarForTenantAsync(_repository, request.LeafCalendarId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (leafResult.IsFailure)
        {
            return Result.Failure<HolidayResolutionDto>(leafResult.Error);
        }

        var chain = await _repository
            .ListLayerChainAsync(new HolidayCalendarId(request.LeafCalendarId), cancellationToken)
            .ConfigureAwait(false);

        var resolution = HolidayResolver.Resolve(chain, request.Date);
        return Result.Success(TimekeepingMapper.ToDto(resolution));
    }
}

/// <summary>
/// Resolves the schedule governing an organizational unit on a date, selecting the
/// version in force on that date rather than the current one (TK-002). This is the
/// query CTR-DAT-005 names for this module: revise a schedule effective next month,
/// ask about a date last month, and the prior version must come back unchanged.
/// </summary>
public sealed record ResolveScheduleForOrganizationalUnitOnDateQuery(
    Guid TenantId, Guid LineageId, OrganizationalAssignmentLevel TargetLevel, string? TargetId, DateOnly Date)
    : IQuery<Result<WorkScheduleDto>>;

internal sealed class ResolveScheduleForOrganizationalUnitOnDateQueryHandler
    : IRequestHandler<ResolveScheduleForOrganizationalUnitOnDateQuery, Result<WorkScheduleDto>>
{
    private readonly IWorkScheduleRepository _repository;

    public ResolveScheduleForOrganizationalUnitOnDateQueryHandler(IWorkScheduleRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<WorkScheduleDto>> Handle(
        ResolveScheduleForOrganizationalUnitOnDateQuery request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var versions = await _repository.ListByLineageAsync(request.LineageId, cancellationToken).ConfigureAwait(false);

        var governing = versions
            .Where(schedule => schedule.TenantId == request.TenantId)
            .Where(schedule => schedule.Status != WorkScheduleStatus.Draft)
            .Where(schedule => schedule.IsEffectiveOn(request.Date))
            .Where(schedule => string.IsNullOrWhiteSpace(request.TargetId)
                || schedule.AssignmentOn(request.TargetLevel, request.TargetId.Trim(), request.Date) is not null)
            .OrderByDescending(schedule => schedule.EffectiveFrom)
            .FirstOrDefault();

        return governing is null
            ? Result.Failure<WorkScheduleDto>(TimekeepingErrors.WorkScheduleNotFound)
            : Result.Success(TimekeepingMapper.ToDto(governing));
    }
}

/// <summary>
/// Resolves which single work date a shift instance's hours belong to, given the
/// date it started. Exists as its own query because <c>attendance</c> needs the
/// answer without loading and re-deriving the shift's timing, and because one place
/// computing it is the whole reason the anchor rule lives on the shift.
/// </summary>
public sealed record ResolveWorkDateForShiftInstantQuery(Guid TenantId, Guid WorkShiftId, DateOnly ShiftStartDate)
    : IQuery<Result<DateOnly>>;

internal sealed class ResolveWorkDateForShiftInstantQueryHandler
    : IRequestHandler<ResolveWorkDateForShiftInstantQuery, Result<DateOnly>>
{
    private readonly IWorkShiftRepository _repository;

    public ResolveWorkDateForShiftInstantQueryHandler(IWorkShiftRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<DateOnly>> Handle(
        ResolveWorkDateForShiftInstantQuery request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var shiftResult = await TimekeepingLookup
            .LoadShiftForTenantAsync(_repository, request.WorkShiftId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        return shiftResult.IsFailure
            ? Result.Failure<DateOnly>(shiftResult.Error)
            : Result.Success(shiftResult.Value.ResolveWorkDate(request.ShiftStartDate));
    }
}
