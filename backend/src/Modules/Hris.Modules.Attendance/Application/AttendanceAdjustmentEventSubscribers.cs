using Hris.Foundation.Events.Domain;
using Hris.Modules.Attendance.Application.Commands;
using Hris.Modules.Attendance.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Attendance.Application;

/// <summary>
/// The in-process reaction side of the two-phase adjustment (application/command-handlers.md,
/// "The Two-Phase Adjustment Handlers"). Each <see cref="IDomainEventSubscriber{TEvent}"/> is the
/// "separate, independently triggered" handler the docs describe: it issues the follow-up command
/// against the <em>other</em> aggregate in its own transaction, so approval and application never
/// share a write (AT-024). Subscribers are idempotent per CTR-WFL-003 — at-least-once delivery is
/// expected and redelivery must not double-apply.
///
/// These subscribe through <see cref="IDomainEventSubscriber{TEvent}"/> (the platform's framework-
/// agnostic contract), not MediatR's <c>INotificationHandler</c>, per the Events foundation's own
/// guidance. The outbox dispatcher opens a fresh DI scope per poll, so each subscriber invocation
/// runs in its own scope and therefore its own transaction boundary.
/// </summary>
public sealed class AttendanceAdjustmentSubmittedSubscriber : IDomainEventSubscriber<AttendanceAdjustmentSubmitted>
{
    private readonly ISender _sender;

    public AttendanceAdjustmentSubmittedSubscriber(ISender sender) => _sender = Guard.AgainstNull(sender, nameof(sender));

    public Task HandleAsync(AttendanceAdjustmentSubmitted domainEvent, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(domainEvent, nameof(domainEvent));

        return _sender.Send(
            new MarkRecordAdjustmentSubmittedCommand(domainEvent.TenantId, domainEvent.AttendanceRecordId.Value),
            cancellationToken);
    }
}

public sealed class AttendanceAdjustmentApprovedSubscriber : IDomainEventSubscriber<AttendanceAdjustmentApproved>
{
    private readonly ISender _sender;

    public AttendanceAdjustmentApprovedSubscriber(ISender sender) => _sender = Guard.AgainstNull(sender, nameof(sender));

    public Task HandleAsync(AttendanceAdjustmentApproved domainEvent, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(domainEvent, nameof(domainEvent));

        return _sender.Send(
            new ApplyAttendanceAdjustmentCommand(
                domainEvent.TenantId, domainEvent.AttendanceRecordId.Value, domainEvent.AttendanceAdjustmentId.Value,
                domainEvent.Field, domainEvent.RequestedValue),
            cancellationToken);
    }
}

public sealed class AttendanceAdjustmentAppliedSubscriber : IDomainEventSubscriber<AttendanceAdjustmentApplied>
{
    private readonly ISender _sender;

    public AttendanceAdjustmentAppliedSubscriber(ISender sender) => _sender = Guard.AgainstNull(sender, nameof(sender));

    public Task HandleAsync(AttendanceAdjustmentApplied domainEvent, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(domainEvent, nameof(domainEvent));

        return _sender.Send(
            new MarkAdjustmentAppliedCommand(domainEvent.TenantId, domainEvent.AttendanceAdjustmentId.Value),
            cancellationToken);
    }
}

public sealed class AttendanceAdjustmentRejectedSubscriber : IDomainEventSubscriber<AttendanceAdjustmentRejected>
{
    private readonly ISender _sender;

    public AttendanceAdjustmentRejectedSubscriber(ISender sender) => _sender = Guard.AgainstNull(sender, nameof(sender));

    public Task HandleAsync(AttendanceAdjustmentRejected domainEvent, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(domainEvent, nameof(domainEvent));

        return _sender.Send(
            new ResolveRecordAdjustmentCommand(domainEvent.TenantId, domainEvent.AttendanceRecordId.Value),
            cancellationToken);
    }
}

public sealed class AttendanceAdjustmentCancelledSubscriber : IDomainEventSubscriber<AttendanceAdjustmentCancelled>
{
    private readonly ISender _sender;

    public AttendanceAdjustmentCancelledSubscriber(ISender sender) => _sender = Guard.AgainstNull(sender, nameof(sender));

    public Task HandleAsync(AttendanceAdjustmentCancelled domainEvent, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(domainEvent, nameof(domainEvent));

        return _sender.Send(
            new ResolveRecordAdjustmentCommand(domainEvent.TenantId, domainEvent.AttendanceRecordId.Value),
            cancellationToken);
    }
}
