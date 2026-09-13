using Hris.Foundation.Events.Domain;
using Hris.Modules.Leave.Application.Commands;
using Hris.Modules.Leave.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Leave.Application;

/// <summary>
/// The in-process reaction side of the two-phase request approval/cancellation (LV-040,
/// LV-041), mirroring <c>Hris.Modules.Attendance.Application.AttendanceAdjustmentSubmittedSubscriber</c>'s
/// own shape: each <see cref="IDomainEventSubscriber{TEvent}"/> issues the follow-up
/// command against <c>LeaveBalance</c> — the other aggregate — in its own transaction, so
/// approval/cancellation and the balance effect never share a write. Subscribers are
/// idempotent per CTR-WFL-003 — at-least-once delivery is expected and redelivery must not
/// double-apply (the target <c>LeaveBalance</c> commands are themselves idempotent-by-
/// source-reference wherever the aggregate's own methods enforce it).
/// </summary>
public sealed class LeaveApprovedSubscriber : IDomainEventSubscriber<LeaveApproved>
{
    private readonly ISender _sender;

    public LeaveApprovedSubscriber(ISender sender) => _sender = Guard.AgainstNull(sender, nameof(sender));

    public Task HandleAsync(LeaveApproved domainEvent, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(domainEvent, nameof(domainEvent));

        if (domainEvent.PaidDays <= 0)
        {
            return Task.CompletedTask;
        }

        return _sender.Send(
            new ApplyLeaveBalanceDeductionCommand(
                domainEvent.TenantId, domainEvent.EmployeeId, domainEvent.LeaveTypeId.Value, domainEvent.LeaveRequestId.Value,
                domainEvent.PaidDays, domainEvent.DateRange.StartDate, domainEvent.ApproverId, AllowNegativeBalance: false),
            cancellationToken);
    }
}

public sealed class LeaveCancelledSubscriber : IDomainEventSubscriber<LeaveCancelled>
{
    private readonly ISender _sender;

    public LeaveCancelledSubscriber(ISender sender) => _sender = Guard.AgainstNull(sender, nameof(sender));

    public Task HandleAsync(LeaveCancelled domainEvent, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(domainEvent, nameof(domainEvent));

        if (!domainEvent.WasApproved || domainEvent.PaidDaysToRestore <= 0)
        {
            return Task.CompletedTask;
        }

        return _sender.Send(
            new ApplyLeaveBalanceCompensationCommand(
                domainEvent.TenantId, domainEvent.EmployeeId, domainEvent.LeaveTypeId.Value, domainEvent.LeaveRequestId.Value,
                domainEvent.PaidDaysToRestore, domainEvent.DateRange.StartDate, domainEvent.ActorId),
            cancellationToken);
    }
}
