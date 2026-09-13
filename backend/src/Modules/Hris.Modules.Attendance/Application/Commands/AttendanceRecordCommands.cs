using Hris.Application.Abstractions;
using Hris.Modules.Attendance.Application;
using Hris.Modules.Attendance.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Attendance.Application.Commands;

/// <summary>
/// Captures one time event. Creates the employee's <see cref="AttendanceRecord"/> for the
/// work date if none exists yet, then appends the event (idempotent per commands.md).
/// Source: application/commands.md (AttendanceRecord Commands).
/// </summary>
public sealed record CaptureTimeEventCommand(
    Guid TenantId,
    Guid EmployeeId,
    DateOnly WorkDate,
    TimeEventType EventType,
    DateTimeOffset TimestampUtc,
    AttendanceSource Source,
    Guid? AttendanceDeviceId,
    string? RawValue,
    string? OriginatingTimeZone,
    Guid? ActorId) : ICommand<Result<Guid>>;

internal sealed class CaptureTimeEventCommandHandler : IRequestHandler<CaptureTimeEventCommand, Result<Guid>>
{
    private readonly IAttendanceRecordRepository _repository;
    private readonly IAttendanceDeviceRepository _deviceRepository;
    private readonly TimeProvider _timeProvider;

    public CaptureTimeEventCommandHandler(
        IAttendanceRecordRepository repository, IAttendanceDeviceRepository deviceRepository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _deviceRepository = Guard.AgainstNull(deviceRepository, nameof(deviceRepository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(CaptureTimeEventCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var nowUtc = _timeProvider.GetUtcNow();
        AttendanceRecord? record = await _repository
            .GetByEmployeeAndWorkDateAsync(request.TenantId, request.EmployeeId, request.WorkDate, cancellationToken)
            .ConfigureAwait(false);

        var isNew = record is null;
        if (record is null)
        {
            var createResult = AttendanceRecord.Create(
                new AttendanceRecordId(Guid.NewGuid()), request.TenantId, request.EmployeeId, request.WorkDate,
                nowUtc, request.ActorId);
            if (createResult.IsFailure)
            {
                return Result.Failure<Guid>(createResult.Error);
            }

            record = createResult.Value;
        }

        // AT-050: a device-originated event is only accepted from an Active device. This is a
        // read-only existence/status check of a second aggregate, not a modification of it.
        if (request.AttendanceDeviceId.HasValue)
        {
            var device = await _deviceRepository
                .GetByIdAsync(new AttendanceDeviceId(request.AttendanceDeviceId.Value), cancellationToken)
                .ConfigureAwait(false);
            if (device is not null && !device.CanSubmitEvents())
            {
                return Result.Failure<Guid>(AttendanceErrors.DeviceNotActive);
            }
        }

        var captureResult = record.CaptureTimeEvent(
            new TimeEventId(Guid.NewGuid()), request.EventType, request.TimestampUtc, request.Source,
            request.AttendanceDeviceId, request.RawValue, request.OriginatingTimeZone, nowUtc, request.ActorId);
        if (captureResult.IsFailure)
        {
            return Result.Failure<Guid>(captureResult.Error);
        }

        if (isNew)
        {
            await _repository.AddAsync(record, cancellationToken).ConfigureAwait(false);
        }

        return Result.Success(record.Id.Value);
    }
}

/// <summary>
/// Runs the calculation pipeline against a record's captured events. Timekeeping's shift and
/// holiday determination arrive already resolved (<see cref="ResolvedWorkShiftId"/> through
/// <see cref="HolidayType"/>) rather than being looked up here — CTR-ARC-002/003 forbid this
/// module from referencing Timekeeping's project or types directly, so the caller (whatever
/// composes both modules — no module has an HTTP endpoint wired yet, this platform-wide, not
/// specific to this command) dispatches Timekeeping's own <c>ResolveShiftForEmployeeOnDateQuery</c>
/// and <c>ResolveHolidayForScopeOnDateQuery</c> first and hands in the results. This handler
/// resolves only what is legitimately this module's own: the effective policy version.
/// The handler assembles all of it into a single <see cref="CalculationInputs"/> and lets the
/// stateless <see cref="AttendanceCalculationEngine"/> derive the result. Actor is optional: a
/// scheduled recalculation carries none (AT-071).
/// Source: application/commands.md, command-handlers.md (The Calculation Handler).
/// </summary>
public sealed record RunCalculationCommand(
    Guid TenantId,
    Guid AttendanceRecordId,
    string Trigger,
    IReadOnlyList<string> CandidateTargetIds,
    Guid? HolidayCalendarId,
    Guid? ResolvedWorkShiftId,
    bool IsShiftUnresolved,
    bool IsShiftAmbiguous,
    bool IsHoliday,
    string? HolidayType,
    Guid? ActorId) : ICommand<Result>;

internal sealed class RunCalculationCommandHandler : IRequestHandler<RunCalculationCommand, Result>
{
    private readonly IAttendanceRecordRepository _repository;
    private readonly IOvertimeRequestRepository _overtimeRepository;
    private readonly IAttendancePolicyRepository _policyRepository;
    private readonly TimeProvider _timeProvider;

    public RunCalculationCommandHandler(
        IAttendanceRecordRepository repository,
        IOvertimeRequestRepository overtimeRepository,
        IAttendancePolicyRepository policyRepository,
        TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _overtimeRepository = Guard.AgainstNull(overtimeRepository, nameof(overtimeRepository));
        _policyRepository = Guard.AgainstNull(policyRepository, nameof(policyRepository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(RunCalculationCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var recordResult = await AttendanceLookup
            .LoadRecordForTenantAsync(_repository, request.AttendanceRecordId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (recordResult.IsFailure)
        {
            return Result.Failure(recordResult.Error);
        }

        var record = recordResult.Value;
        var nowUtc = _timeProvider.GetUtcNow();

        var candidates = await _policyRepository
            .ListEffectiveForWorkDateAsync(request.TenantId, record.WorkDate, cancellationToken)
            .ConfigureAwait(false);
        var policy = ResolveEffectivePolicy(candidates, request.CandidateTargetIds, record.WorkDate);
        if (policy is null)
        {
            return Result.Failure(AttendanceErrors.AttendancePolicyNotFound);
        }

        var approvedOvertime = (await _overtimeRepository
                .ListApprovedForWorkDateAsync(request.TenantId, record.WorkDate, cancellationToken)
                .ConfigureAwait(false))
            .Select(ToSummary)
            .ToList();

        var inputs = new CalculationInputs(
            request.ResolvedWorkShiftId,
            null,
            null,
            request.IsShiftUnresolved,
            request.IsShiftAmbiguous,
            request.IsHoliday,
            request.HolidayType,
            approvedOvertime,
            policy.Configuration);

        var result = AttendanceCalculationEngine.Calculate(record.TimeEvents, record.WorkDate, inputs, nowUtc);

        var applyResult = record.ApplyCalculation(result, nowUtc);
        if (applyResult.IsFailure)
        {
            return Result.Failure(applyResult.Error);
        }

        record.NoteResolvedReferences(request.ResolvedWorkShiftId, request.HolidayCalendarId);

        return Result.Success();
    }

    private static AttendancePolicy? ResolveEffectivePolicy(
        IReadOnlyList<AttendancePolicy> candidates, IReadOnlyList<string> targetIds, DateOnly workDate) =>
        candidates.FirstOrDefault(policy => policy.PolicyAssignments.Any(assignment =>
            targetIds.Contains(assignment.ScopeTargetId, StringComparer.Ordinal) && assignment.IsEffectiveOn(workDate)));

    private static OvertimeRequestSummary ToSummary(OvertimeRequest request) =>
        new(request.Id, request.WorkDate, request.PlannedStart, request.PlannedEnd, request.Category, request.EstimatedHours);
}

/// <summary>Submits a calculated record for approval (requires AT-030 to have passed).</summary>
public sealed record SubmitAttendanceForApprovalCommand(Guid TenantId, Guid AttendanceRecordId, Guid ActorId)
    : ICommand<Result>;

internal sealed class SubmitAttendanceForApprovalCommandHandler
    : IRequestHandler<SubmitAttendanceForApprovalCommand, Result>
{
    private readonly IAttendanceRecordRepository _repository;
    private readonly TimeProvider _timeProvider;

    public SubmitAttendanceForApprovalCommandHandler(IAttendanceRecordRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(SubmitAttendanceForApprovalCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var recordResult = await AttendanceLookup
            .LoadRecordForTenantAsync(_repository, request.AttendanceRecordId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (recordResult.IsFailure)
        {
            return Result.Failure(recordResult.Error);
        }

        return recordResult.Value.Submit(request.ActorId, _timeProvider.GetUtcNow());
    }
}

/// <summary>Approves a submitted record, recording the approval-chain decision.</summary>
public sealed record ApproveAttendanceCommand(
    Guid TenantId,
    Guid AttendanceRecordId,
    Guid ApproverId,
    ApprovalDecisionOutcome Outcome,
    string? Comments,
    Guid? DelegateId,
    Guid? OriginalApproverId) : ICommand<Result>;

internal sealed class ApproveAttendanceCommandHandler : IRequestHandler<ApproveAttendanceCommand, Result>
{
    private readonly IAttendanceRecordRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ApproveAttendanceCommandHandler(IAttendanceRecordRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ApproveAttendanceCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var recordResult = await AttendanceLookup
            .LoadRecordForTenantAsync(_repository, request.AttendanceRecordId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (recordResult.IsFailure)
        {
            return Result.Failure(recordResult.Error);
        }

        var nowUtc = _timeProvider.GetUtcNow();
        var decision = new ApprovalDecision(
            request.ApproverId, request.Outcome, nowUtc, request.Comments, request.DelegateId, request.OriginalApproverId);

        return recordResult.Value.Approve(request.ApproverId, decision, nowUtc);
    }
}

/// <summary>Rejects a submitted record, returning it for correction.</summary>
public sealed record RejectAttendanceCommand(Guid TenantId, Guid AttendanceRecordId, Guid ApproverId, string Reason)
    : ICommand<Result>;

internal sealed class RejectAttendanceCommandHandler : IRequestHandler<RejectAttendanceCommand, Result>
{
    private readonly IAttendanceRecordRepository _repository;
    private readonly TimeProvider _timeProvider;

    public RejectAttendanceCommandHandler(IAttendanceRecordRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(RejectAttendanceCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var recordResult = await AttendanceLookup
            .LoadRecordForTenantAsync(_repository, request.AttendanceRecordId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (recordResult.IsFailure)
        {
            return Result.Failure(recordResult.Error);
        }

        return recordResult.Value.Reject(request.ApproverId, request.Reason, _timeProvider.GetUtcNow());
    }
}

/// <summary>
/// Finalizes a record into its payroll-stable state. Refused while an adjustment is pending
/// (AT-031) and idempotent once already finalized.
/// </summary>
public sealed record FinalizeAttendanceCommand(Guid TenantId, Guid AttendanceRecordId, Guid ActorId) : ICommand<Result>;

internal sealed class FinalizeAttendanceCommandHandler : IRequestHandler<FinalizeAttendanceCommand, Result>
{
    private readonly IAttendanceRecordRepository _repository;
    private readonly TimeProvider _timeProvider;

    public FinalizeAttendanceCommandHandler(IAttendanceRecordRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(FinalizeAttendanceCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var recordResult = await AttendanceLookup
            .LoadRecordForTenantAsync(_repository, request.AttendanceRecordId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (recordResult.IsFailure)
        {
            return Result.Failure(recordResult.Error);
        }

        return recordResult.Value.Finalize(request.ActorId, _timeProvider.GetUtcNow());
    }
}

/// <summary>Lifts finalization so the record can be corrected and recalculated (AT-032).</summary>
public sealed record ReopenAttendanceCommand(
    Guid TenantId, Guid AttendanceRecordId, Guid ActorId, string AuthorizationReference, string Reason)
    : ICommand<Result>;

internal sealed class ReopenAttendanceCommandHandler : IRequestHandler<ReopenAttendanceCommand, Result>
{
    private readonly IAttendanceRecordRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ReopenAttendanceCommandHandler(IAttendanceRecordRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ReopenAttendanceCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var recordResult = await AttendanceLookup
            .LoadRecordForTenantAsync(_repository, request.AttendanceRecordId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (recordResult.IsFailure)
        {
            return Result.Failure(recordResult.Error);
        }

        return recordResult.Value.Reopen(request.ActorId, request.AuthorizationReference, request.Reason, _timeProvider.GetUtcNow());
    }
}

/// <summary>
/// System command issued by the <c>AttendanceAdjustmentSubmitted</c> integration handler to
/// mark the targeted record's pending-adjustment block (AT-031). Never dispatched by a caller
/// directly.
/// </summary>
public sealed record MarkRecordAdjustmentSubmittedCommand(Guid TenantId, Guid AttendanceRecordId) : ICommand<Result>;

internal sealed class MarkRecordAdjustmentSubmittedCommandHandler
    : IRequestHandler<MarkRecordAdjustmentSubmittedCommand, Result>
{
    private readonly IAttendanceRecordRepository _repository;

    public MarkRecordAdjustmentSubmittedCommandHandler(IAttendanceRecordRepository repository) =>
        _repository = Guard.AgainstNull(repository, nameof(repository));

    public async Task<Result> Handle(MarkRecordAdjustmentSubmittedCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var recordResult = await AttendanceLookup
            .LoadRecordForTenantAsync(_repository, request.AttendanceRecordId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (recordResult.IsFailure)
        {
            return Result.Failure(recordResult.Error);
        }

        recordResult.Value.MarkAdjustmentSubmitted();
        return Result.Success();
    }
}

/// <summary>
/// System command issued by the <c>AttendanceAdjustmentRejected</c> / <c>AttendanceAdjustmentCancelled</c>
/// integration handlers to release a record's pending-adjustment block when the adjustment is
/// withdrawn (AT-031).
/// </summary>
public sealed record ResolveRecordAdjustmentCommand(Guid TenantId, Guid AttendanceRecordId) : ICommand<Result>;

internal sealed class ResolveRecordAdjustmentCommandHandler : IRequestHandler<ResolveRecordAdjustmentCommand, Result>
{
    private readonly IAttendanceRecordRepository _repository;

    public ResolveRecordAdjustmentCommandHandler(IAttendanceRecordRepository repository) =>
        _repository = Guard.AgainstNull(repository, nameof(repository));

    public async Task<Result> Handle(ResolveRecordAdjustmentCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var recordResult = await AttendanceLookup
            .LoadRecordForTenantAsync(_repository, request.AttendanceRecordId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (recordResult.IsFailure)
        {
            return Result.Failure(recordResult.Error);
        }

        recordResult.Value.MarkAdjustmentResolved();
        return Result.Success();
    }
}
