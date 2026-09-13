using Hris.Modules.Attendance.Domain;
using Hris.SharedKernel;

namespace Hris.Modules.Attendance.Domain;

/// <summary>
/// The authoritative record of one employee's captured, calculated, and approved time
/// for one work date. Source: docs/04-modules/attendance/domain/aggregates.md
/// (AttendanceRecord) and attendance-records.md.
///
/// It owns its <see cref="TimeEvent"/> set, the <see cref="CalculatedFields"/> the
/// calculation engine derives from that set, and its own lifecycle
/// (Created → Calculated → Submitted → Approved → Finalized). The resolved shift and
/// holiday references are stored by identifier only — they belong to Timekeeping, which
/// this module queries but never owns (CTR-ARC-003). An approved
/// <see cref="AttendanceAdjustment"/> is applied to this record in a transaction
/// separate from its approval (AT-024).
/// </summary>
public sealed class AttendanceRecord : AggregateRoot<AttendanceRecordId>
{
    public Guid TenantId { get; }

    public Guid EmployeeId { get; }

    public DateOnly WorkDate { get; }

    private readonly List<TimeEvent> _timeEvents = new();

    /// <summary>Immutable punches; corrected only through an adjustment (AT-001).</summary>
    public IReadOnlyList<TimeEvent> TimeEvents => _timeEvents.AsReadOnly();

    /// <summary>Resolved shift identifier from Timekeeping; read by id, never owned.</summary>
    public Guid? WorkShiftId { get; private set; }

    /// <summary>Resolved holiday calendar identifier from Timekeeping; read by id, never owned.</summary>
    public Guid? HolidayCalendarId { get; private set; }

    public CalculatedFields? Calculated { get; private set; }

    private readonly List<string> _exceptions = new();

    /// <summary>Validation or calculation exceptions the pipeline flagged rather than resolved.</summary>
    public IReadOnlyList<string> Exceptions => _exceptions.AsReadOnly();

    public AttendanceStatus Status { get; private set; }

    public ApprovalStatus ApprovalStatus { get; private set; }

    public PayrollStatus PayrollStatus { get; private set; }

    private readonly List<AppliedAdjustment> _appliedAdjustments = new();

    /// <summary>Read-only projection of adjustments already applied to this record.</summary>
    public IReadOnlyList<AppliedAdjustment> AppliedAdjustments => _appliedAdjustments.AsReadOnly();

    /// <summary>Count of adjustments currently pending against this record; blocks finalization (AT-031).</summary>
    public int PendingAdjustmentCount { get; private set; }

    private AttendanceRecord(AttendanceRecordId id, Guid tenantId, Guid employeeId, DateOnly workDate)
        : base(id)
    {
        TenantId = tenantId;
        EmployeeId = employeeId;
        WorkDate = workDate;
        Status = AttendanceStatus.Created;
        ApprovalStatus = ApprovalStatus.Draft;
        PayrollStatus = PayrollStatus.NotProcessed;
    }

    /// <summary>Authors the record, or the first record for an employee/date pair.</summary>
    public static Result<AttendanceRecord> Create(
        AttendanceRecordId id, Guid tenantId, Guid employeeId, DateOnly workDate,
        DateTimeOffset createdOnUtc, Guid? actorId)
    {
        if (employeeId == Guid.Empty)
        {
            return Result.Failure<AttendanceRecord>(AttendanceErrors.EmployeeIdentifierRequired);
        }

        var record = new AttendanceRecord(id, tenantId, employeeId, workDate);
        record.AddDomainEvent(new AttendanceRecordCreated(
            Guid.NewGuid(), createdOnUtc, id, tenantId, employeeId, workDate, actorId));
        return Result.Success(record);
    }

    /// <summary>
    /// Appends a captured event. Idempotent by event type, source, and a one-minute
    /// window (per commands.md), and refused once the record is finalized or archived
    /// (AT-032). The correction path for a bad capture is an adjustment, never an edit
    /// of this event (AT-001).
    /// </summary>
    public Result CaptureTimeEvent(
        TimeEventId eventId, TimeEventType eventType, DateTimeOffset timestampUtc, AttendanceSource source,
        Guid? attendanceDeviceId, string? rawValue, string? originatingTimeZone,
        DateTimeOffset capturedOnUtc, Guid? actorId)
    {
        if (Status is AttendanceStatus.Finalized or AttendanceStatus.Archived)
        {
            return Result.Failure(AttendanceErrors.RecordAlreadyFinalized);
        }

        var isDuplicate = _timeEvents.Any(existing =>
            existing.EventType == eventType
            && existing.Source == source
            && Math.Abs((existing.TimestampUtc - timestampUtc).TotalMinutes) < 1);

        if (isDuplicate)
        {
            return Result.Failure(AttendanceErrors.DuplicateTimeEvent);
        }

        var timeEvent = new TimeEvent(
            eventId, eventType, timestampUtc, source, attendanceDeviceId, rawValue, originatingTimeZone);
        _timeEvents.Add(timeEvent);

        AddDomainEvent(new TimeEventCaptured(
            Guid.NewGuid(), capturedOnUtc, Id, TenantId, eventId, eventType, source, timestampUtc));

        return Result.Success();
    }

    /// <summary>
    /// Writes the result of a calculation run. Kept separate from
    /// <see cref="AttendanceCalculationEngine"/> so the aggregate depends on the engine's
    /// output type, not on the engine itself; the handler runs the engine and calls this.
    /// Refused once finalized or archived (AT-032).
    /// </summary>
    public Result ApplyCalculation(CalculationResult result, DateTimeOffset calculatedAtUtc)
    {
        Guard.AgainstNull(result, nameof(result));

        if (Status is AttendanceStatus.Finalized or AttendanceStatus.Archived)
        {
            return Result.Failure(AttendanceErrors.RecordAlreadyFinalized);
        }

        Calculated = result.Fields;
        _exceptions.Clear();
        _exceptions.AddRange(result.Exceptions);
        Status = AttendanceStatus.Calculated;

        AddDomainEvent(new AttendanceRecordCalculated(
            Guid.NewGuid(), calculatedAtUtc, Id, TenantId,
            result.Fields.WorkingHours, result.Fields.PayableHours, result.Fields.OvertimeHours,
            result.Fields.LateMinutes, result.Fields.UndertimeMinutes, result.Fields.HolidayHours,
            result.Fields.NightDifferentialHours, result.Trigger, null));

        return Result.Success();
    }

    /// <summary>Requires the record to have been calculated/validated first (AT-030).</summary>
    public Result Submit(Guid actorId, DateTimeOffset nowUtc)
    {
        if (Status != AttendanceStatus.Calculated)
        {
            return Result.Failure(AttendanceErrors.RecordNotValidated);
        }

        Status = AttendanceStatus.Submitted;
        ApprovalStatus = ApprovalStatus.Pending;
        AddDomainEvent(new AttendanceRecordSubmittedForApproval(Guid.NewGuid(), nowUtc, Id, TenantId, actorId));
        return Result.Success();
    }

    public Result Approve(Guid approverId, ApprovalDecision decision, DateTimeOffset nowUtc)
    {
        if (Status != AttendanceStatus.Submitted)
        {
            return Result.Failure(AttendanceErrors.InvalidStateTransition);
        }

        Status = AttendanceStatus.Approved;
        ApprovalStatus = ApprovalStatus.Approved;
        AddDomainEvent(new AttendanceRecordApproved(Guid.NewGuid(), nowUtc, Id, TenantId, approverId));
        return Result.Success();
    }

    /// <summary>Returns the record for correction; the pending adjustment that prompted it remains authoritative.</summary>
    public Result Reject(Guid approverId, string reason, DateTimeOffset nowUtc)
    {
        if (Status != AttendanceStatus.Submitted)
        {
            return Result.Failure(AttendanceErrors.InvalidStateTransition);
        }

        Status = AttendanceStatus.Calculated;
        ApprovalStatus = ApprovalStatus.Rejected;
        AddDomainEvent(new AttendanceRecordRejected(Guid.NewGuid(), nowUtc, Id, TenantId, approverId, reason));
        return Result.Success();
    }

    /// <summary>
    /// Lifts the record into its immutable, payroll-stable state. Refused while an
    /// adjustment is pending (AT-031) and idempotent once already finalized.
    /// </summary>
    public Result Finalize(Guid actorId, DateTimeOffset nowUtc)
    {
        if (Status == AttendanceStatus.Finalized)
        {
            return Result.Success();
        }

        if (Status != AttendanceStatus.Approved)
        {
            return Result.Failure(AttendanceErrors.InvalidStateTransition);
        }

        if (PendingAdjustmentCount > 0)
        {
            return Result.Failure(AttendanceErrors.PendingAdjustmentBlocksFinalization);
        }

        Status = AttendanceStatus.Finalized;
        AddDomainEvent(new AttendanceRecordFinalized(Guid.NewGuid(), nowUtc, Id, TenantId, actorId));
        return Result.Success();
    }

    /// <summary>
    /// Lifts finalization so the record can be corrected and recalculated (AT-032). The
    /// reopening itself changes no value other than status; it authorizes the next
    /// change, it is not the change.
    /// </summary>
    public Result Reopen(Guid actorId, string authorizationReference, string reason, DateTimeOffset nowUtc)
    {
        if (Status != AttendanceStatus.Finalized)
        {
            return Result.Failure(AttendanceErrors.InvalidStateTransition);
        }

        Status = AttendanceStatus.Calculated;
        ApprovalStatus = ApprovalStatus.Draft;
        AddDomainEvent(new AttendanceRecordReopened(
            Guid.NewGuid(), nowUtc, Id, TenantId, actorId, authorizationReference, reason));
        return Result.Success();
    }

    /// <summary>
    /// Applies an already-approved adjustment's outcome to this record (AT-024), in the
    /// transaction opened by <c>ApplyAttendanceAdjustmentCommand</c>. Decrements the
    /// pending count and records a projection of the applied change.
    /// </summary>
    public Result ApplyAdjustment(
        AttendanceAdjustmentId adjustmentId, string field, string requestedValue, DateTimeOffset nowUtc)
    {
        Guard.AgainstNull(field, nameof(field));

        if (Status is AttendanceStatus.Finalized or AttendanceStatus.Archived)
        {
            return Result.Failure(AttendanceErrors.RecordAlreadyFinalized);
        }

        if (Calculated is not null && double.TryParse(requestedValue, out var value))
        {
            // ToUpperInvariant, not ToLowerInvariant, per CA1308 -- .NET's case-insensitive
            // comparison tables are built around the upper-invariant form.
            Calculated = field.Trim().ToUpperInvariant() switch
            {
                "WORKINGHOURS" => Calculated with { WorkingHours = value },
                "PAYABLEHOURS" => Calculated with { PayableHours = value },
                "OVERTIMEHOURS" => Calculated with { OvertimeHours = value },
                "LATEMINUTES" => Calculated with { LateMinutes = value },
                "UNDERTIMEMINUTES" => Calculated with { UndertimeMinutes = value },
                "HOLIDAYHOURS" => Calculated with { HolidayHours = value },
                "NIGHTDIFFERENTIALHOURS" => Calculated with { NightDifferentialHours = value },
                _ => Calculated,
            };
        }

        _appliedAdjustments.Add(new AppliedAdjustment(adjustmentId, field, requestedValue, nowUtc));

        if (PendingAdjustmentCount > 0)
        {
            PendingAdjustmentCount--;
        }

        AddDomainEvent(new AttendanceAdjustmentApplied(Guid.NewGuid(), nowUtc, adjustmentId, TenantId, Id));
        return Result.Success();
    }

    /// <summary>Stores the resolved shift/holiday identifiers after a calculation run, so they are explainable later.</summary>
    internal void NoteResolvedReferences(Guid? workShiftId, Guid? holidayCalendarId)
    {
        WorkShiftId = workShiftId;
        HolidayCalendarId = holidayCalendarId;
    }

    /// <summary>Called by the handler that reacts to an adjustment being submitted (AT-031).</summary>
    internal void MarkAdjustmentSubmitted() => PendingAdjustmentCount++;

    /// <summary>Called when a pending adjustment is cancelled or rejected, releasing the block.</summary>
    internal void MarkAdjustmentResolved()
    {
        if (PendingAdjustmentCount > 0)
        {
            PendingAdjustmentCount--;
        }
    }
}
