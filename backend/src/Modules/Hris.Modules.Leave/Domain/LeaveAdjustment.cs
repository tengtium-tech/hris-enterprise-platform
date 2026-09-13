using Hris.SharedKernel;

namespace Hris.Modules.Leave.Domain;

/// <summary>
/// A manual, authorized correction to a <see cref="LeaveBalance"/> — a migration
/// correction, a manual entitlement grant outside normal accrual, or a fix to an error
/// discovered elsewhere. Its own first-class, reviewable action, never a privileged
/// direct-write permission on <c>LeaveBalance</c>. Mirrors
/// <c>Hris.Modules.Attendance.Domain.AttendanceAdjustment</c>'s seven-state lifecycle and
/// two-phase approve/apply shape exactly, per leave-adjustments.md's own stated reasoning
/// — this module's own type, not a reference to Attendance's (CTR-ARC-002). Source:
/// docs/04-modules/leave/domain/aggregates.md, leave-adjustments.md.
///
/// Available only from Leave pack maturity Level 2 (LV-050) — an entitlement gate the
/// submitting command's handler evaluates before authorization, never here; this aggregate
/// has no way to observe entitlement itself. Approval requires <c>HRManager</c>
/// authorization (LV-051), also outside this aggregate's own concern. Snapshots the
/// balance state it corrects at submission, never a live reference re-read at approval
/// (LV-054).
/// </summary>
public sealed class LeaveAdjustment : AggregateRoot<LeaveAdjustmentId>
{
    public Guid TenantId { get; }

    public LeaveBalanceId LeaveBalanceId { get; }

    /// <summary><c>LeaveBalance.CurrentBalance</c> at submission time — a snapshot, never re-read live (LV-054).</summary>
    public decimal OriginalValueSnapshot { get; }

    public decimal RequestedAmount { get; }

    public string Reason { get; }

    public IReadOnlyList<string> SupportingDocuments { get; }

    public LeaveAdjustmentStatus Status { get; private set; }

    public Guid SubmittedBy { get; }

    public DateTimeOffset SubmittedOn { get; }

    public Guid? ReviewerId { get; private set; }

    public string? ReviewNotes { get; private set; }

    public ApprovalDecision? Decision { get; private set; }

    public string? RejectionReason { get; private set; }

    public DateTimeOffset? AppliedAt { get; private set; }

    private LeaveAdjustment(
        LeaveAdjustmentId id, Guid tenantId, LeaveBalanceId leaveBalanceId, decimal originalValueSnapshot,
        decimal requestedAmount, string reason, IReadOnlyList<string> supportingDocuments, Guid submittedBy,
        DateTimeOffset submittedOn)
        : base(id)
    {
        TenantId = tenantId;
        LeaveBalanceId = leaveBalanceId;
        OriginalValueSnapshot = originalValueSnapshot;
        RequestedAmount = requestedAmount;
        Reason = reason;
        SupportingDocuments = supportingDocuments;
        SubmittedBy = submittedBy;
        SubmittedOn = submittedOn;
        Status = LeaveAdjustmentStatus.Draft;
    }

    public static Result<LeaveAdjustment> Create(
        LeaveAdjustmentId id, Guid tenantId, LeaveBalanceId leaveBalanceId, decimal originalValueSnapshot,
        decimal requestedAmount, string? reason, IReadOnlyList<string>? supportingDocuments, Guid submittedBy,
        DateTimeOffset submittedOnUtc)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result.Failure<LeaveAdjustment>(LeaveErrors.AdjustmentReasonRequired);
        }

        if (requestedAmount == 0)
        {
            return Result.Failure<LeaveAdjustment>(LeaveErrors.LedgerEntryAmountMustNotBeZero);
        }

        var adjustment = new LeaveAdjustment(
            id, tenantId, leaveBalanceId, originalValueSnapshot, requestedAmount, reason.Trim(), supportingDocuments ?? [],
            submittedBy, submittedOnUtc);
        adjustment.AddDomainEvent(new LeaveAdjustmentSubmitted(Guid.NewGuid(), submittedOnUtc, adjustment.Id, tenantId, leaveBalanceId, submittedBy));
        return Result.Success(adjustment);
    }

    public Result Submit(DateTimeOffset submittedOnUtc)
    {
        _ = submittedOnUtc;

        if (Status != LeaveAdjustmentStatus.Draft)
        {
            return Result.Failure(LeaveErrors.AdjustmentNotInReviewableState);
        }

        Status = LeaveAdjustmentStatus.Submitted;
        return Result.Success();
    }

    public Result Review(Guid reviewerId, string notes, DateTimeOffset reviewedOnUtc)
    {
        _ = reviewedOnUtc;

        if (Status != LeaveAdjustmentStatus.Submitted)
        {
            return Result.Failure(LeaveErrors.AdjustmentNotInReviewableState);
        }

        Status = LeaveAdjustmentStatus.UnderReview;
        ReviewerId = reviewerId;
        ReviewNotes = notes;
        return Result.Success();
    }

    /// <summary>Approvable from Draft, Submitted, or Under Review (leave-adjustments.md). Idempotent once Approved or Applied.</summary>
    public Result Approve(Guid approverId, ApprovalDecision decision, DateTimeOffset approvedOnUtc)
    {
        if (Status is LeaveAdjustmentStatus.Approved or LeaveAdjustmentStatus.Applied)
        {
            return Result.Success();
        }

        if (Status is not (LeaveAdjustmentStatus.Draft or LeaveAdjustmentStatus.Submitted or LeaveAdjustmentStatus.UnderReview))
        {
            return Result.Failure(LeaveErrors.AdjustmentNotInReviewableState);
        }

        Status = LeaveAdjustmentStatus.Approved;
        Decision = decision;
        AddDomainEvent(new LeaveAdjustmentApproved(
            Guid.NewGuid(), approvedOnUtc, Id, TenantId, LeaveBalanceId, RequestedAmount, approverId));
        return Result.Success();
    }

    public Result Reject(Guid approverId, string reason, DateTimeOffset rejectedOnUtc)
    {
        if (Status is not (LeaveAdjustmentStatus.Draft or LeaveAdjustmentStatus.Submitted or LeaveAdjustmentStatus.UnderReview))
        {
            return Result.Failure(LeaveErrors.AdjustmentNotInReviewableState);
        }

        Status = LeaveAdjustmentStatus.Rejected;
        RejectionReason = reason;
        AddDomainEvent(new LeaveAdjustmentRejected(Guid.NewGuid(), rejectedOnUtc, Id, TenantId, approverId, reason));
        return Result.Success();
    }

    /// <summary>
    /// Withdrawable from any non-terminal state, including after approval but before
    /// application. No <c>LeaveAdjustmentCancelled</c> domain event exists in
    /// domain-events.md's own catalog — the actor and reason an
    /// <c>CancelLeaveAdjustmentCommand</c> otherwise carries are for the audit record the
    /// command dispatch itself produces.
    /// </summary>
    public Result Cancel(DateTimeOffset cancelledOnUtc)
    {
        _ = cancelledOnUtc;

        if (Status is LeaveAdjustmentStatus.Applied or LeaveAdjustmentStatus.Cancelled)
        {
            return Result.Failure(LeaveErrors.AdjustmentNotInReviewableState);
        }

        Status = LeaveAdjustmentStatus.Cancelled;
        return Result.Success();
    }

    /// <summary>Set only after <c>LeaveBalance</c>'s own transaction confirms incorporation (LV-052) — never optimistically at approval.</summary>
    public Result MarkApplied(DateTimeOffset appliedOnUtc)
    {
        if (Status == LeaveAdjustmentStatus.Applied)
        {
            return Result.Success();
        }

        if (Status != LeaveAdjustmentStatus.Approved)
        {
            return Result.Failure(LeaveErrors.AdjustmentNotApproved);
        }

        Status = LeaveAdjustmentStatus.Applied;
        AppliedAt = appliedOnUtc;
        return Result.Success();
    }
}
