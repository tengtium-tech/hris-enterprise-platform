using Hris.SharedKernel;

namespace Hris.Modules.Leave.Domain;

/// <summary>
/// A voluntary, employee-or-HR-initiated request to convert unused commutable balance
/// into pay. Kept independent of <see cref="LeaveAdjustment"/> so its own eligibility check
/// and approval authority never blur with adjustment's error-correction authority — the
/// segregation of duties leave-encashments.md requires structurally, mirroring how
/// <c>Hris.Modules.Attendance.Domain.OvertimeRequest</c> stays independent of
/// <c>AttendanceAdjustment</c>. Source: docs/04-modules/leave/domain/aggregates.md,
/// leave-encashments.md.
///
/// Available only from Leave pack maturity Level 3 (LV-074) — the highest gate of any
/// capability in this module, evaluated by the submitting command's handler before
/// authorization, never here. Commutability and sufficiency (LV-070) are validated at
/// submission — unlike <see cref="LeaveRequest"/>, which is deliberately accepted even
/// where it will later fail eligibility, an encashment request is rejected outright rather
/// than accepted and failed at approval. Submitting never itself affects
/// <c>LeaveBalance</c> (LV-071); only approval does, through a separate transaction.
/// </summary>
public sealed class LeaveEncashment : AggregateRoot<LeaveEncashmentId>
{
    public Guid TenantId { get; }

    public Guid EmployeeId { get; }

    public LeaveBalanceId LeaveBalanceId { get; }

    public decimal RequestedAmount { get; }

    public LeaveEncashmentStatus Status { get; private set; }

    public Guid SubmittedBy { get; }

    public DateTimeOffset SubmittedOn { get; }

    public ApprovalDecision? Decision { get; private set; }

    public string? RejectionReason { get; private set; }

    public string? CancellationReason { get; private set; }

    private LeaveEncashment(
        LeaveEncashmentId id, Guid tenantId, Guid employeeId, LeaveBalanceId leaveBalanceId, decimal requestedAmount,
        Guid submittedBy, DateTimeOffset submittedOn)
        : base(id)
    {
        TenantId = tenantId;
        EmployeeId = employeeId;
        LeaveBalanceId = leaveBalanceId;
        RequestedAmount = requestedAmount;
        SubmittedBy = submittedBy;
        SubmittedOn = submittedOn;
    }

    /// <summary>
    /// <paramref name="isCommutable"/>, <paramref name="maximumCommutable"/>, and
    /// <paramref name="availableBalance"/> are the caller's own resolution of the effective
    /// <c>LeavePolicy</c>'s <c>CommutabilityStatus</c> and the target balance's current
    /// total — this aggregate never reads either directly (Aggregate Design Rule 13).
    /// LV-072's duplicate-pending check spans multiple aggregate instances and is enforced
    /// by the submitting command's handler, not here.
    /// </summary>
    public static Result<LeaveEncashment> Create(
        LeaveEncashmentId id, Guid tenantId, Guid employeeId, LeaveBalanceId leaveBalanceId, decimal requestedAmount,
        bool isCommutable, decimal? maximumCommutable, decimal availableBalance, Guid submittedBy, DateTimeOffset submittedOnUtc)
    {
        if (employeeId == Guid.Empty)
        {
            return Result.Failure<LeaveEncashment>(LeaveErrors.EmployeeIdentifierRequired);
        }

        if (requestedAmount <= 0)
        {
            return Result.Failure<LeaveEncashment>(LeaveErrors.LedgerEntryAmountMustBePositive);
        }

        if (!isCommutable)
        {
            return Result.Failure<LeaveEncashment>(LeaveErrors.EncashmentNotCommutable);
        }

        if (maximumCommutable.HasValue && requestedAmount > maximumCommutable.Value)
        {
            return Result.Failure<LeaveEncashment>(LeaveErrors.EncashmentExceedsCommutableCap);
        }

        if (requestedAmount > availableBalance)
        {
            return Result.Failure<LeaveEncashment>(LeaveErrors.InsufficientBalance);
        }

        var encashment = new LeaveEncashment(id, tenantId, employeeId, leaveBalanceId, requestedAmount, submittedBy, submittedOnUtc)
        {
            Status = LeaveEncashmentStatus.PendingApproval,
        };
        encashment.AddDomainEvent(new LeaveEncashmentRequested(Guid.NewGuid(), submittedOnUtc, encashment.Id, tenantId, employeeId, submittedBy));
        return Result.Success(encashment);
    }

    /// <summary>Approves the request; never itself writes to <c>LeaveBalance</c> (LV-071).</summary>
    public Result Approve(Guid approverId, ApprovalDecision decision, DateTimeOffset approvedOnUtc)
    {
        if (Status != LeaveEncashmentStatus.PendingApproval)
        {
            return Result.Failure(LeaveErrors.EncashmentNotPendingApproval);
        }

        Status = LeaveEncashmentStatus.Approved;
        Decision = decision;
        AddDomainEvent(new LeaveEncashmentApproved(
            Guid.NewGuid(), approvedOnUtc, Id, TenantId, LeaveBalanceId, RequestedAmount, approverId));
        return Result.Success();
    }

    public Result Reject(Guid approverId, string reason, DateTimeOffset rejectedOnUtc)
    {
        if (Status != LeaveEncashmentStatus.PendingApproval)
        {
            return Result.Failure(LeaveErrors.EncashmentNotPendingApproval);
        }

        Status = LeaveEncashmentStatus.Rejected;
        RejectionReason = reason;
        AddDomainEvent(new LeaveEncashmentRejected(Guid.NewGuid(), rejectedOnUtc, Id, TenantId, approverId, reason));
        return Result.Success();
    }

    /// <summary>
    /// Cancellable only while pending — leave-encashments.md's own lifecycle diagram draws
    /// Cancelled as a branch from Pending Approval only, unlike <see cref="LeaveRequest.Cancel"/>'s
    /// after-the-fact case, since an Approved encashment has no balance effect of its own
    /// to reverse until <c>payroll</c>'s own future Processed/Paid steps exist.
    /// </summary>
    public Result Cancel(Guid actorId, string reason, DateTimeOffset cancelledOnUtc)
    {
        if (Status != LeaveEncashmentStatus.PendingApproval)
        {
            return Result.Failure(LeaveErrors.EncashmentNotCancellable);
        }

        Status = LeaveEncashmentStatus.Cancelled;
        CancellationReason = reason;
        AddDomainEvent(new LeaveEncashmentCancelled(Guid.NewGuid(), cancelledOnUtc, Id, TenantId, actorId, reason));
        return Result.Success();
    }
}
