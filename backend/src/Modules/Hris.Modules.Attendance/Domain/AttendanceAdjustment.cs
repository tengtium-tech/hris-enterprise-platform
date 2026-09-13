using Hris.SharedKernel;

namespace Hris.Modules.Attendance.Domain;

/// <summary>
/// One correction request and its own seven-state review lifecycle, an independent
/// aggregate root (not an entity inside <see cref="AttendanceRecord"/>). Source:
/// docs/04-modules/attendance/domain/aggregates.md (AttendanceAdjustment) and
/// application/commands.md.
///
/// Approval and application are two transactions against two roots, linked only by
/// <see cref="AttendanceAdjustmentApproved"/> (AT-024): <c>ApproveAttendanceAdjustmentCommand</c>
/// raises that event; a separate transaction then runs <c>ApplyAttendanceAdjustmentCommand</c>
/// against the record, and the record's resulting <see cref="AttendanceAdjustmentApplied"/>
/// event marks this adjustment Applied. The original value is snapshotted at submission,
/// never read live at approval (AT-020).
/// </summary>
public sealed class AttendanceAdjustment : AggregateRoot<AttendanceAdjustmentId>
{
    public Guid TenantId { get; }

    public AttendanceRecordId AttendanceRecordId { get; }

    public DateOnly WorkDate { get; }

    public string Field { get; }

    /// <summary>Snapshot of the value as it stood at submission, never a live reference (AT-020).</summary>
    public string OriginalValue { get; }

    public string RequestedValue { get; }

    public AdjustmentCategory Category { get; }

    public string Reason { get; }

    private readonly List<string> _supportingDocuments = new();

    public IReadOnlyList<string> SupportingDocuments => _supportingDocuments.AsReadOnly();

    public Guid SubmittedBy { get; }

    public DateTimeOffset SubmittedOn { get; }

    public AdjustmentStatus Status { get; private set; }

    public string? ReviewNotes { get; private set; }

    public Guid? ReviewerId { get; private set; }

    public Guid? ApproverId { get; private set; }

    public ApprovalDecision? Decision { get; private set; }

    public DateTimeOffset? ApprovedOn { get; private set; }

    private AttendanceAdjustment(
        AttendanceAdjustmentId id, Guid tenantId, AttendanceRecordId attendanceRecordId, DateOnly workDate,
        string field, string originalValue, string requestedValue, AdjustmentCategory category, string reason,
        IReadOnlyList<string>? supportingDocuments, Guid submittedBy, DateTimeOffset submittedOn)
        : base(id)
    {
        TenantId = tenantId;
        AttendanceRecordId = attendanceRecordId;
        WorkDate = workDate;
        Field = field;
        OriginalValue = originalValue;
        RequestedValue = requestedValue;
        Category = category;
        Reason = reason;
        _supportingDocuments = supportingDocuments?.ToList() ?? new List<string>();
        SubmittedBy = submittedBy;
        SubmittedOn = submittedOn;
        Status = AdjustmentStatus.Draft;
    }

    public static Result<AttendanceAdjustment> Create(
        AttendanceAdjustmentId id, Guid tenantId, AttendanceRecordId attendanceRecordId, DateOnly workDate,
        string field, string originalValue, string requestedValue, AdjustmentCategory category, string reason,
        IReadOnlyList<string>? supportingDocuments, Guid submittedBy, DateTimeOffset submittedOn)
    {
        if (string.IsNullOrWhiteSpace(field))
        {
            return Result.Failure<AttendanceAdjustment>(AttendanceErrors.OriginalValueSnapshotRequired);
        }

        var adjustment = new AttendanceAdjustment(
            id, tenantId, attendanceRecordId, workDate, field, originalValue, requestedValue, category, reason,
            supportingDocuments, submittedBy, submittedOn);

        adjustment.AddDomainEvent(new AttendanceAdjustmentSubmitted(
            Guid.NewGuid(), submittedOn, id, tenantId, attendanceRecordId, submittedBy));

        return Result.Success(adjustment);
    }

    public Result Submit(Guid actorId, DateTimeOffset nowUtc)
    {
        if (Status != AdjustmentStatus.Draft)
        {
            return Result.Failure(AttendanceErrors.AdjustmentNotInReviewableState);
        }

        Status = AdjustmentStatus.Submitted;
        return Result.Success();
    }

    public Result Review(Guid reviewerId, string notes, DateTimeOffset nowUtc)
    {
        if (Status != AdjustmentStatus.Submitted)
        {
            return Result.Failure(AttendanceErrors.AdjustmentNotInReviewableState);
        }

        Status = AdjustmentStatus.UnderReview;
        ReviewerId = reviewerId;
        ReviewNotes = notes;
        AddDomainEvent(new AttendanceAdjustmentReviewed(
            Guid.NewGuid(), nowUtc, Id, TenantId, reviewerId, notes));
        return Result.Success();
    }

    /// <summary>
    /// Moves to Approved and raises <see cref="AttendanceAdjustmentApproved"/>, which the
    /// separate application transaction consumes (AT-024). Idempotent once already Approved.
    /// </summary>
    public Result Approve(Guid approverId, ApprovalDecision decision, DateTimeOffset nowUtc)
    {
        if (Status is AdjustmentStatus.Approved or AdjustmentStatus.Applied)
        {
            return Result.Success();
        }

        if (Status is AdjustmentStatus.Rejected or AdjustmentStatus.Cancelled)
        {
            return Result.Failure(AttendanceErrors.AdjustmentNotInReviewableState);
        }

        Status = AdjustmentStatus.Approved;
        ApproverId = approverId;
        Decision = decision;
        ApprovedOn = nowUtc;
        AddDomainEvent(new AttendanceAdjustmentApproved(
            Guid.NewGuid(), nowUtc, Id, TenantId, AttendanceRecordId, approverId, RequestedValue, Field));
        return Result.Success();
    }

    public Result Reject(Guid approverId, string reason, DateTimeOffset nowUtc)
    {
        if (Status is AdjustmentStatus.Rejected or AdjustmentStatus.Cancelled or AdjustmentStatus.Applied)
        {
            return Result.Failure(AttendanceErrors.AdjustmentNotInReviewableState);
        }

        Status = AdjustmentStatus.Rejected;
        ApproverId = approverId;
        AddDomainEvent(new AttendanceAdjustmentRejected(Guid.NewGuid(), nowUtc, Id, TenantId, AttendanceRecordId, approverId, reason));
        return Result.Success();
    }

    public Result Cancel(Guid actorId, string reason, DateTimeOffset nowUtc)
    {
        if (Status is AdjustmentStatus.Applied or AdjustmentStatus.Cancelled)
        {
            return Result.Failure(AttendanceErrors.AdjustmentNotInReviewableState);
        }

        Status = AdjustmentStatus.Cancelled;
        AddDomainEvent(new AttendanceAdjustmentCancelled(Guid.NewGuid(), nowUtc, Id, TenantId, AttendanceRecordId, actorId, reason));
        return Result.Success();
    }

    /// <summary>
    /// Reached only by the transaction that reacts to <see cref="AttendanceAdjustmentApplied"/>,
    /// confirming the record incorporated the change. Never set optimistically before the
    /// record's own transaction commits (AT-024). Idempotent once Applied.
    /// </summary>
    internal Result MarkApplied()
    {
        if (Status == AdjustmentStatus.Applied)
        {
            return Result.Success();
        }

        if (Status != AdjustmentStatus.Approved)
        {
            return Result.Failure(AttendanceErrors.AdjustmentNotApproved);
        }

        Status = AdjustmentStatus.Applied;
        return Result.Success();
    }
}
