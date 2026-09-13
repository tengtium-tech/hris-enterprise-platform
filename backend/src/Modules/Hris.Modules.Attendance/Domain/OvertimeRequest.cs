using Hris.SharedKernel;

namespace Hris.Modules.Attendance.Domain;

/// <summary>
/// One pre-authorization request for overtime and its own approval lifecycle, an
/// independent aggregate root. Source: docs/04-modules/attendance/domain/aggregates.md
/// (OvertimeRequest) and application/commands.md.
///
/// It stands apart from <see cref="AttendanceRecord"/> because it can be submitted and
/// approved before any attendance exists for the work date (AT-040); where the effective
/// <see cref="AttendancePolicy"/> requires prior authorization, the calculation engine
/// checks for an Approved request for the relevant hours rather than the record
/// performing the work. A rejected or cancelled request is never payable (AT-041).
/// </summary>
public sealed class OvertimeRequest : AggregateRoot<OvertimeRequestId>
{
    public Guid TenantId { get; }

    public Guid EmployeeId { get; }

    public DateOnly WorkDate { get; }

    public TimeOnly? PlannedStart { get; }

    public TimeOnly? PlannedEnd { get; }

    public double EstimatedHours { get; }

    public OvertimeCategory Category { get; }

    public string Justification { get; }

    public ApprovalStatus Status { get; private set; }

    public Guid? ApproverId { get; private set; }

    public DateTimeOffset? DecidedOn { get; private set; }

    public string? RejectionReason { get; private set; }

    private OvertimeRequest(
        OvertimeRequestId id, Guid tenantId, Guid employeeId, DateOnly workDate, TimeOnly? plannedStart,
        TimeOnly? plannedEnd, double estimatedHours, OvertimeCategory category, string justification)
        : base(id)
    {
        TenantId = tenantId;
        EmployeeId = employeeId;
        WorkDate = workDate;
        PlannedStart = plannedStart;
        PlannedEnd = plannedEnd;
        EstimatedHours = estimatedHours;
        Category = category;
        Justification = justification;
        Status = ApprovalStatus.Draft;
    }

    public static Result<OvertimeRequest> Create(
        OvertimeRequestId id, Guid tenantId, Guid employeeId, DateOnly workDate, TimeOnly? plannedStart,
        TimeOnly? plannedEnd, double estimatedHours, OvertimeCategory category, string justification,
        Guid submittedBy, DateTimeOffset submittedOn)
    {
        if (employeeId == Guid.Empty)
        {
            return Result.Failure<OvertimeRequest>(AttendanceErrors.EmployeeIdentifierRequired);
        }

        var request = new OvertimeRequest(
            id, tenantId, employeeId, workDate, plannedStart, plannedEnd, estimatedHours, category, justification);

        request.AddDomainEvent(new OvertimeRequestSubmitted(
            Guid.NewGuid(), submittedOn, id, tenantId, employeeId, workDate, submittedBy));

        return Result.Success(request);
    }

    public Result Approve(Guid approverId, DateTimeOffset nowUtc)
    {
        if (Status is ApprovalStatus.Approved)
        {
            return Result.Success();
        }

        if (Status is ApprovalStatus.Rejected or ApprovalStatus.Cancelled)
        {
            return Result.Failure(AttendanceErrors.OvertimeRequestNotApproved);
        }

        Status = ApprovalStatus.Approved;
        ApproverId = approverId;
        DecidedOn = nowUtc;
        AddDomainEvent(new OvertimeRequestApproved(Guid.NewGuid(), nowUtc, Id, TenantId, approverId));
        return Result.Success();
    }

    public Result Reject(Guid approverId, string reason, DateTimeOffset nowUtc)
    {
        if (Status is ApprovalStatus.Rejected or ApprovalStatus.Cancelled or ApprovalStatus.Approved)
        {
            return Result.Failure(AttendanceErrors.OvertimeRequestNotApproved);
        }

        Status = ApprovalStatus.Rejected;
        ApproverId = approverId;
        DecidedOn = nowUtc;
        RejectionReason = reason;
        AddDomainEvent(new OvertimeRequestRejected(Guid.NewGuid(), nowUtc, Id, TenantId, approverId, reason));
        return Result.Success();
    }

    public Result Cancel(Guid actorId, string reason, DateTimeOffset nowUtc)
    {
        if (Status is ApprovalStatus.Cancelled or ApprovalStatus.Approved)
        {
            return Result.Failure(AttendanceErrors.OvertimeRequestNotApproved);
        }

        Status = ApprovalStatus.Cancelled;
        AddDomainEvent(new OvertimeRequestCancelled(Guid.NewGuid(), nowUtc, Id, TenantId, actorId, reason));
        return Result.Success();
    }
}
