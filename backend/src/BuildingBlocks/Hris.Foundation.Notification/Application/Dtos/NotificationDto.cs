namespace Hris.Foundation.Notification.Application.Dtos;

public sealed record NotificationDto(
    Guid NotificationId,
    Guid TenantId,
    Guid RecipientUserId,
    string NotificationType,
    string Channel,
    string? TemplateKey,
    string? Subject,
    string Body,
    string Status,
    DateTimeOffset RequestedAtUtc,
    DateTimeOffset? ScheduledForUtc,
    DateTimeOffset? SentAtUtc,
    DateTimeOffset? DeliveredAtUtc,
    DateTimeOffset? ReadAtUtc,
    DateTimeOffset? AcknowledgedAtUtc,
    string? FailureReason,
    int RetryCount,
    string? CancellationReason);
