using Hris.SharedKernel;

namespace Hris.Foundation.Notification.Domain;

/// <summary>
/// notification-framework.md's own Domain Events section names exactly eight events --
/// every one implemented here, split seven/one across <see cref="Notification"/> and
/// <see cref="NotificationTemplate"/>. Not every <see cref="NotificationStatus"/>
/// transition raises one: the document names no event for Scheduled, Processing,
/// Acknowledged, Expired, Suppressed, or DeadLetter, the same asymmetric-event-list
/// pattern every other framework in this codebase already follows where a document's own
/// named list is shorter than its own state diagram.
/// </summary>
public sealed record NotificationRequested(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    NotificationId NotificationId,
    Guid TenantId,
    Guid RecipientUserId,
    NotificationType NotificationType,
    NotificationChannel Channel) : IDomainEvent;

public sealed record NotificationQueued(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    NotificationId NotificationId) : IDomainEvent;

public sealed record NotificationSent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    NotificationId NotificationId) : IDomainEvent;

public sealed record NotificationDelivered(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    NotificationId NotificationId) : IDomainEvent;

public sealed record NotificationRead(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    NotificationId NotificationId,
    Guid RecipientUserId) : IDomainEvent;

public sealed record NotificationFailed(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    NotificationId NotificationId,
    string Reason) : IDomainEvent;

public sealed record NotificationCancelled(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    NotificationId NotificationId,
    string Reason) : IDomainEvent;

public sealed record NotificationTemplateUpdated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    NotificationTemplateId NotificationTemplateId,
    int VersionNumber) : IDomainEvent;
