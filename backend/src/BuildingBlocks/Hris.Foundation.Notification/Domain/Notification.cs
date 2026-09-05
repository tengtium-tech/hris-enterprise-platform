using System.Diagnostics.CodeAnalysis;
using Hris.SharedKernel;

namespace Hris.Foundation.Notification.Domain;

/// <summary>
/// Aggregate Root for one notification's own request-through-delivery journey on one
/// channel, per notification-framework.md's own Notification Lifecycle and Delivery
/// Tracking sections. Population-scale ("Support millions of notifications daily," NFR
/// Scalability), never nested under a config aggregate.
///
/// <see cref="TenantId"/> and <see cref="RecipientUserId"/> are both plain
/// <see cref="Guid"/>s, caller-supplied -- built concretely, per this document's own AI
/// Implementation Guidance naming CTR-ISO-004 explicitly: "Resolve recipients within
/// tenant context; a notification must never address a user in another tenant."
///
/// There is deliberately no client-facing factory that lets an arbitrary caller create
/// one of these: notification-framework.md's own Client-Facing Commands and Queries
/// section states plainly "There is no CreateNotificationCommand here... Notifications
/// are created by business modules publishing notification requests." <see cref="Request"/>
/// is reachable only from another module's own MediatR command, never a client-facing
/// API surface this Sprint's own build exposes.
///
/// Every constructor parameter shares its name with the property it sets, the proactive
/// naming discipline every Sprint 4/5 aggregate after Search Framework already
/// establishes, confirmed by a real EF Core model build needing no second constructor.
/// </summary>
[SuppressMessage(
    "Naming",
    "CA1724:Type names should not match namespaces",
    Justification = "\"Notification\" is notification-framework.md's own ubiquitous-language " +
        "name for this Aggregate Root and this project's own name (Hris.Foundation.Notification) " +
        "matches every sibling Hris.Foundation.<Framework> project in this solution -- renaming " +
        "either would break one of those two established conventions to satisfy a naming lint, " +
        "the identical justification Tenant's own CA1724 suppression already establishes for " +
        "itself in this codebase.")]
public sealed class Notification : AggregateRoot<NotificationId>
{
    public Guid TenantId { get; }

    public Guid RecipientUserId { get; }

    public NotificationType NotificationType { get; }

    public NotificationChannel Channel { get; }

    public string? TemplateKey { get; }

    public string? Subject { get; }

    public string Body { get; }

    public NotificationStatus Status { get; private set; }

    public DateTimeOffset RequestedAtUtc { get; }

    public DateTimeOffset? ScheduledForUtc { get; private set; }

    public DateTimeOffset? SentAtUtc { get; private set; }

    public DateTimeOffset? DeliveredAtUtc { get; private set; }

    public DateTimeOffset? ReadAtUtc { get; private set; }

    public DateTimeOffset? AcknowledgedAtUtc { get; private set; }

    public string? FailureReason { get; private set; }

    public int RetryCount { get; private set; }

    public string? CancellationReason { get; private set; }

    private Notification(
        NotificationId id,
        Guid tenantId,
        Guid recipientUserId,
        NotificationType notificationType,
        NotificationChannel channel,
        string? templateKey,
        string? subject,
        string body,
        DateTimeOffset requestedAtUtc)
        : base(id)
    {
        TenantId = tenantId;
        RecipientUserId = recipientUserId;
        NotificationType = notificationType;
        Channel = channel;
        TemplateKey = templateKey;
        Subject = subject;
        Body = body;
        Status = NotificationStatus.Created;
        RequestedAtUtc = requestedAtUtc;
        RetryCount = 0;
    }

    /// <summary>
    /// Records a new notification request, in <see cref="NotificationStatus.Created"/>.
    /// Raises <see cref="NotificationRequested"/>. <paramref name="body"/> is already-
    /// resolved content -- the publishing module (or a template-rendering step upstream
    /// of this framework) substitutes Template Variables before calling this factory;
    /// this Sprint's own build does not implement the substitution engine itself, the
    /// same "records the configuration, does not build the runtime that walks it" split
    /// this framework's own <c>NotificationTemplateVersion.Body</c> remarks state for
    /// the template side of the same problem.
    /// </summary>
    public static Result<Domain.Notification> Request(
        Guid tenantId,
        Guid recipientUserId,
        NotificationType notificationType,
        NotificationChannel channel,
        string? templateKey,
        string? subject,
        string? body,
        DateTimeOffset requestedAtUtc)
    {
        Guard.AgainstDefault(tenantId, nameof(tenantId));
        Guard.AgainstDefault(recipientUserId, nameof(recipientUserId));

        if (string.IsNullOrWhiteSpace(body))
        {
            return Result.Failure<Domain.Notification>(NotificationErrors.BodyRequired);
        }

        var notification = new Domain.Notification(
            new NotificationId(Guid.NewGuid()), tenantId, recipientUserId, notificationType, channel,
            string.IsNullOrWhiteSpace(templateKey) ? null : templateKey.Trim(), subject, body.Trim(), requestedAtUtc);

        notification.AddDomainEvent(new NotificationRequested(
            Guid.NewGuid(), requestedAtUtc, notification.Id, tenantId, recipientUserId, notificationType, channel));

        return Result.Success(notification);
    }

    public Result Queue(DateTimeOffset nowUtc)
    {
        if (Status != NotificationStatus.Created)
        {
            return Result.Failure(NotificationErrors.InvalidNotificationLifecycleTransition);
        }

        Status = NotificationStatus.Queued;
        AddDomainEvent(new NotificationQueued(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    /// <summary>
    /// Schedules delivery for a future point in time -- notification-framework.md's own
    /// Scheduling section ("Scheduled, Recurring, Delayed"). Raises no event: this
    /// document's own Domain Events list names none for this transition.
    /// </summary>
    public Result Schedule(DateTimeOffset scheduledForUtc)
    {
        if (Status != NotificationStatus.Queued)
        {
            return Result.Failure(NotificationErrors.InvalidNotificationLifecycleTransition);
        }

        Status = NotificationStatus.Scheduled;
        ScheduledForUtc = scheduledForUtc;
        return Result.Success();
    }

    public Result StartProcessing()
    {
        if (Status is not (NotificationStatus.Queued or NotificationStatus.Scheduled))
        {
            return Result.Failure(NotificationErrors.InvalidNotificationLifecycleTransition);
        }

        Status = NotificationStatus.Processing;
        return Result.Success();
    }

    public Result MarkSent(DateTimeOffset nowUtc)
    {
        if (Status != NotificationStatus.Processing)
        {
            return Result.Failure(NotificationErrors.InvalidNotificationLifecycleTransition);
        }

        Status = NotificationStatus.Sent;
        SentAtUtc = nowUtc;
        AddDomainEvent(new NotificationSent(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    public Result MarkDelivered(DateTimeOffset nowUtc)
    {
        if (Status != NotificationStatus.Sent)
        {
            return Result.Failure(NotificationErrors.InvalidNotificationLifecycleTransition);
        }

        Status = NotificationStatus.Delivered;
        DeliveredAtUtc = nowUtc;
        AddDomainEvent(new NotificationDelivered(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    /// <summary>
    /// Backs <c>MarkNotificationReadCommand</c>. Scoped to the caller's own notification
    /// unconditionally -- notification-framework.md's own Client-Facing Commands
    /// section: "There is no admin-override path... inventing one would create a way to
    /// read another user's notification content." Enforced here, structurally, on the
    /// Domain method itself rather than only in the Application-layer handler, per
    /// CLAUDE.md's own "prefer structure over discipline" principle -- an invariant this
    /// load-bearing should not depend on every future caller remembering a check.
    ///
    /// Idempotent, per this document's own explicit requirement: marking an already-read
    /// notification succeeds without change and without re-raising the event -- the
    /// identical convergence-on-retry requirement every mark-complete-shaped command in
    /// this codebase already follows.
    /// </summary>
    public Result MarkRead(Guid actorUserId, DateTimeOffset nowUtc)
    {
        if (actorUserId != RecipientUserId)
        {
            return Result.Failure(NotificationErrors.NotAuthorizedForThisNotification);
        }

        if (Status == NotificationStatus.Read)
        {
            return Result.Success();
        }

        if (Status != NotificationStatus.Delivered)
        {
            return Result.Failure(NotificationErrors.InvalidNotificationLifecycleTransition);
        }

        Status = NotificationStatus.Read;
        ReadAtUtc = nowUtc;
        AddDomainEvent(new NotificationRead(Guid.NewGuid(), nowUtc, Id, actorUserId));
        return Result.Success();
    }

    public Result Acknowledge(DateTimeOffset nowUtc)
    {
        if (Status != NotificationStatus.Read)
        {
            return Result.Failure(NotificationErrors.InvalidNotificationLifecycleTransition);
        }

        Status = NotificationStatus.Acknowledged;
        AcknowledgedAtUtc = nowUtc;
        return Result.Success();
    }

    public Result Fail(string? reason, DateTimeOffset nowUtc)
    {
        if (IsTerminal())
        {
            return Result.Failure(NotificationErrors.InvalidNotificationLifecycleTransition);
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result.Failure(NotificationErrors.FailureReasonRequired);
        }

        Status = NotificationStatus.Failed;
        FailureReason = reason.Trim();
        AddDomainEvent(new NotificationFailed(Guid.NewGuid(), nowUtc, Id, reason.Trim()));
        return Result.Success();
    }

    /// <summary>
    /// Retries a failed delivery attempt -- notification-framework.md's own Retry
    /// Processing section ("Automatic Retry... Manual Retry"). Returns to
    /// <see cref="NotificationStatus.Queued"/> and increments <see cref="RetryCount"/>;
    /// raises no event of its own, since re-queueing is not one of this document's own
    /// eight named events -- <see cref="NotificationQueued"/> already fired once, on the
    /// original <see cref="Queue"/> call, and this document names no distinct
    /// "requeued" event.
    /// </summary>
    public Result RetryAfterFailure()
    {
        if (Status != NotificationStatus.Failed)
        {
            return Result.Failure(NotificationErrors.InvalidNotificationLifecycleTransition);
        }

        Status = NotificationStatus.Queued;
        RetryCount++;
        return Result.Success();
    }

    /// <summary>
    /// Exhausted retries land here rather than looping or vanishing silently --
    /// notification-framework.md's own Retry Processing section: "Dead Letter Queue
    /// (DLQ)... Failed deliveries should never silently disappear." Raises no event:
    /// this document's own Domain Events list names none for this transition, the same
    /// asymmetry <c>Job.MoveToDeadLetterQueue</c>'s own remarks note are possible for a
    /// terminal state a document names in prose but not in its own event list.
    /// </summary>
    public Result MoveToDeadLetter()
    {
        if (Status != NotificationStatus.Failed)
        {
            return Result.Failure(NotificationErrors.InvalidNotificationLifecycleTransition);
        }

        Status = NotificationStatus.DeadLetter;
        return Result.Success();
    }

    public Result Expire()
    {
        if (Status is not (NotificationStatus.Queued or NotificationStatus.Scheduled))
        {
            return Result.Failure(NotificationErrors.InvalidNotificationLifecycleTransition);
        }

        Status = NotificationStatus.Expired;
        return Result.Success();
    }

    /// <summary>
    /// A quiet-hours/opt-out worker decision not to deliver at all --
    /// notification-framework.md's own Notification Lifecycle "Alternative outcomes":
    /// Suppressed. Distinct from <see cref="Cancel"/> (an explicit administrative or
    /// publishing-module decision) and <see cref="Fail"/> (a delivery attempt that
    /// actually failed) -- this is "never attempted, by design."
    /// </summary>
    public Result Suppress()
    {
        if (IsTerminal())
        {
            return Result.Failure(NotificationErrors.InvalidNotificationLifecycleTransition);
        }

        Status = NotificationStatus.Suppressed;
        return Result.Success();
    }

    public Result Cancel(string? reason, DateTimeOffset nowUtc)
    {
        if (IsTerminal())
        {
            return Result.Failure(NotificationErrors.InvalidNotificationLifecycleTransition);
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result.Failure(NotificationErrors.CancellationReasonRequired);
        }

        Status = NotificationStatus.Cancelled;
        CancellationReason = reason.Trim();
        AddDomainEvent(new NotificationCancelled(Guid.NewGuid(), nowUtc, Id, reason.Trim()));
        return Result.Success();
    }

    private bool IsTerminal() => Status is NotificationStatus.Delivered or NotificationStatus.Read
        or NotificationStatus.Acknowledged or NotificationStatus.Failed or NotificationStatus.Expired
        or NotificationStatus.Cancelled or NotificationStatus.Suppressed or NotificationStatus.DeadLetter;
}
