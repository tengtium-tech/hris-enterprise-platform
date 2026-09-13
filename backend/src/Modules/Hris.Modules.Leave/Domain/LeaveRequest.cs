using Hris.SharedKernel;

namespace Hris.Modules.Leave.Domain;

/// <summary>
/// An employee's request to take leave, from submission through approval, rejection, or
/// cancellation — the module's primary transactional root. Source:
/// docs/04-modules/leave/domain/aggregates.md, leave-requests.md.
///
/// Approving never itself writes to <c>LeaveBalance</c>; it raises <see cref="LeaveApproved"/>,
/// and <c>LeaveBalance</c>'s own transaction, triggered by that event, appends the
/// deduction separately (LV-040). Cancelling an already-deducted Approved request follows
/// the same shape in reverse via <see cref="LeaveCancelled"/> (LV-041). A request is
/// accepted at submission even where it will later fail eligibility — that failure
/// surfaces as part of the approval decision, computed by the approving command's own
/// handler from the effective <c>LeavePolicy</c> and available balance (both cross-
/// aggregate reads this aggregate itself never performs), never as a silent submission-
/// time rejection.
/// </summary>
public sealed class LeaveRequest : AggregateRoot<LeaveRequestId>
{
    public Guid TenantId { get; }

    public Guid EmployeeId { get; }

    public LeaveTypeId LeaveTypeId { get; }

    public LeaveDateRange DateRange { get; }

    public StatutoryDetails? StatutoryDetails { get; }

    public IReadOnlyList<string> SupportingDocuments { get; }

    public LeaveRequestStatus Status { get; private set; }

    /// <summary>Finalized only at approval, never at submission (LV-037).</summary>
    public PayTreatment? PayTreatment { get; private set; }

    /// <summary>
    /// The portion of <see cref="LeaveDateRange.RequestedDays"/> that actually drew from
    /// <c>LeaveBalance</c> at approval — set once, at <see cref="Approve"/>, and read back
    /// by <see cref="Cancel"/> so a compensating entry restores exactly what was deducted,
    /// never the full requested amount when this request was only partially paid.
    /// </summary>
    public decimal? PaidDays { get; private set; }

    public Guid SubmittedBy { get; }

    public DateTimeOffset SubmittedOn { get; }

    public ApprovalDecision? Decision { get; private set; }

    public string? RejectionReason { get; private set; }

    public string? CancellationReason { get; private set; }

    private LeaveRequest(
        LeaveRequestId id, Guid tenantId, Guid employeeId, LeaveTypeId leaveTypeId, LeaveDateRange dateRange,
        StatutoryDetails? statutoryDetails, IReadOnlyList<string> supportingDocuments, Guid submittedBy,
        DateTimeOffset submittedOn)
        : base(id)
    {
        TenantId = tenantId;
        EmployeeId = employeeId;
        LeaveTypeId = leaveTypeId;
        DateRange = dateRange;
        StatutoryDetails = statutoryDetails;
        SupportingDocuments = supportingDocuments;
        SubmittedBy = submittedBy;
        SubmittedOn = submittedOn;
    }

    /// <summary>
    /// Structural validation only (leave-requests.md): dates are well-formed and the
    /// matching <see cref="StatutoryDetails"/> variant is present where the leave type
    /// requires it (LV-034, <paramref name="requiresStatutoryDetails"/> resolved by the
    /// caller from the referenced <c>LeaveType</c>). LV-031's overlap-against-existing-
    /// requests check spans multiple aggregate instances and is enforced by the submitting
    /// command's handler before this is ever called, not here.
    /// </summary>
    public static Result<LeaveRequest> Create(
        LeaveRequestId id, Guid tenantId, Guid employeeId, LeaveTypeId leaveTypeId, LeaveDateRange dateRange,
        StatutoryDetails? statutoryDetails, bool requiresStatutoryDetails, IReadOnlyList<string>? supportingDocuments,
        Guid submittedBy, DateTimeOffset submittedOnUtc)
    {
        ArgumentNullException.ThrowIfNull(dateRange);

        if (employeeId == Guid.Empty)
        {
            return Result.Failure<LeaveRequest>(LeaveErrors.EmployeeIdentifierRequired);
        }

        if (dateRange.EndDate < dateRange.StartDate)
        {
            return Result.Failure<LeaveRequest>(LeaveErrors.LeaveDateRangeInvalid);
        }

        if (requiresStatutoryDetails && statutoryDetails is null)
        {
            return Result.Failure<LeaveRequest>(LeaveErrors.StatutoryDetailsRequired);
        }

        var request = new LeaveRequest(
            id, tenantId, employeeId, leaveTypeId, dateRange, statutoryDetails, supportingDocuments ?? [], submittedBy,
            submittedOnUtc)
        {
            Status = LeaveRequestStatus.PendingApproval,
        };
        request.AddDomainEvent(new LeaveRequested(Guid.NewGuid(), submittedOnUtc, request.Id, tenantId, employeeId, dateRange, submittedBy));
        return Result.Success(request);
    }

    /// <summary>
    /// Records the decision and finalizes <see cref="PayTreatment"/> (LV-037), computed by
    /// the caller from available balance and the effective policy at approval time — never
    /// re-derived here. <paramref name="paidDays"/> is the portion of
    /// <see cref="LeaveDateRange.RequestedDays"/> that actually draws from
    /// <c>LeaveBalance</c> — less than the full request when <paramref name="payTreatment"/>
    /// is <see cref="Domain.PayTreatment.PartiallyPaid"/>, zero when fully
    /// <see cref="Domain.PayTreatment.Unpaid"/>. Raises <see cref="LeaveApproved"/>; never
    /// writes to <c>LeaveBalance</c> directly (LV-040). Also raises
    /// <see cref="LWOPPeriodRecorded"/> where <paramref name="payTreatment"/> is not fully
    /// <see cref="Domain.PayTreatment.Paid"/> (LV-086) — finalization is exactly this
    /// moment, not a separately invoked command.
    /// </summary>
    public Result Approve(
        Guid approverId, ApprovalDecision decision, PayTreatment payTreatment, decimal paidDays, DateTimeOffset approvedOnUtc)
    {
        if (Status != LeaveRequestStatus.PendingApproval)
        {
            return Result.Failure(LeaveErrors.LeaveRequestNotPendingApproval);
        }

        Status = LeaveRequestStatus.Approved;
        Decision = decision;
        PayTreatment = payTreatment;
        PaidDays = paidDays;
        AddDomainEvent(new LeaveApproved(
            Guid.NewGuid(), approvedOnUtc, Id, TenantId, EmployeeId, LeaveTypeId, DateRange, payTreatment, paidDays, approverId));

        if (payTreatment != Domain.PayTreatment.Paid)
        {
            AddDomainEvent(new LWOPPeriodRecorded(Guid.NewGuid(), approvedOnUtc, Id, TenantId, EmployeeId, DateRange));
        }

        return Result.Success();
    }

    public Result Reject(Guid approverId, string reason, DateTimeOffset rejectedOnUtc)
    {
        if (Status != LeaveRequestStatus.PendingApproval)
        {
            return Result.Failure(LeaveErrors.LeaveRequestNotPendingApproval);
        }

        Status = LeaveRequestStatus.Rejected;
        RejectionReason = reason;
        AddDomainEvent(new LeaveRejected(Guid.NewGuid(), rejectedOnUtc, Id, TenantId, approverId, reason));
        return Result.Success();
    }

    /// <summary>
    /// Cancellable while pending or after approval (LV-041's "after the fact" case);
    /// terminal once Rejected or already Cancelled. The event carries whether this request
    /// had reached Approved, since only then has <c>LeaveBalance</c> already applied a
    /// deduction a compensating entry must reverse.
    /// </summary>
    public Result Cancel(Guid actorId, string reason, DateTimeOffset cancelledOnUtc)
    {
        if (Status is LeaveRequestStatus.Rejected or LeaveRequestStatus.Cancelled)
        {
            return Result.Failure(LeaveErrors.LeaveRequestNotCancellable);
        }

        var wasApproved = Status == LeaveRequestStatus.Approved;
        Status = LeaveRequestStatus.Cancelled;
        CancellationReason = reason;
        AddDomainEvent(new LeaveCancelled(
            Guid.NewGuid(), cancelledOnUtc, Id, TenantId, EmployeeId, LeaveTypeId, DateRange, wasApproved, PaidDays ?? 0, actorId));
        return Result.Success();
    }
}
