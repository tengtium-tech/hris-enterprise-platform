using Hris.Foundation.Events.Domain;
using Hris.Modules.Leave.Application.Commands;
using Hris.Modules.Leave.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Leave.Application;

/// <summary>
/// The in-process reaction side of the two-phase encashment (LV-071), mirroring
/// <c>LeaveAdjustmentApprovedSubscriber</c>'s own shape. No further "Applied" confirmation
/// subscriber is needed here — unlike <c>LeaveAdjustment</c>, LV-073 makes
/// <see cref="LeaveEncashmentStatus.Approved"/> this module's own terminal state for this
/// Sprint, so <c>LeaveBalance.RecordEncashment</c> does not raise a round-trip event.
/// </summary>
public sealed class LeaveEncashmentApprovedSubscriber : IDomainEventSubscriber<LeaveEncashmentApproved>
{
    private readonly ISender _sender;

    public LeaveEncashmentApprovedSubscriber(ISender sender) => _sender = Guard.AgainstNull(sender, nameof(sender));

    public Task HandleAsync(LeaveEncashmentApproved domainEvent, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(domainEvent, nameof(domainEvent));

        return _sender.Send(
            new ApplyLeaveEncashmentCommand(
                domainEvent.TenantId, domainEvent.LeaveBalanceId.Value, domainEvent.LeaveEncashmentId.Value,
                domainEvent.RequestedAmount, DateOnly.FromDateTime(domainEvent.OccurredOnUtc.UtcDateTime), domainEvent.ApproverId),
            cancellationToken);
    }
}
