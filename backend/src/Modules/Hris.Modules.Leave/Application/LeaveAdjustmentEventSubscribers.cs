using Hris.Foundation.Events.Domain;
using Hris.Modules.Leave.Application.Commands;
using Hris.Modules.Leave.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Leave.Application;

/// <summary>
/// The in-process reaction side of the two-phase adjustment (LV-052), mirroring
/// <c>Hris.Modules.Attendance.Application.AttendanceAdjustmentApprovedSubscriber</c>'s own
/// shape exactly.
/// </summary>
public sealed class LeaveAdjustmentApprovedSubscriber : IDomainEventSubscriber<LeaveAdjustmentApproved>
{
    private readonly ISender _sender;

    public LeaveAdjustmentApprovedSubscriber(ISender sender) => _sender = Guard.AgainstNull(sender, nameof(sender));

    public Task HandleAsync(LeaveAdjustmentApproved domainEvent, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(domainEvent, nameof(domainEvent));

        return _sender.Send(
            new ApplyLeaveAdjustmentCommand(
                domainEvent.TenantId, domainEvent.LeaveBalanceId.Value, domainEvent.LeaveAdjustmentId.Value,
                domainEvent.RequestedAmount, DateOnly.FromDateTime(domainEvent.OccurredOnUtc.UtcDateTime), domainEvent.ApproverId),
            cancellationToken);
    }
}

/// <summary>Closes the adjustment to its terminal state once <c>LeaveBalance</c> confirms incorporation.</summary>
public sealed class LeaveAdjustmentAppliedSubscriber : IDomainEventSubscriber<LeaveAdjustmentApplied>
{
    private readonly ISender _sender;

    public LeaveAdjustmentAppliedSubscriber(ISender sender) => _sender = Guard.AgainstNull(sender, nameof(sender));

    public Task HandleAsync(LeaveAdjustmentApplied domainEvent, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(domainEvent, nameof(domainEvent));

        return _sender.Send(
            new MarkLeaveAdjustmentAppliedCommand(domainEvent.TenantId, domainEvent.LeaveAdjustmentId.Value), cancellationToken);
    }
}
