using Hris.Foundation.Notification.Domain;
using NotificationEntity = Hris.Foundation.Notification.Domain.Notification;

namespace Hris.Foundation.Notification.Tests;

/// <summary>
/// Valid-default builders per docs/09-testing/unit-and-integration-testing.md 2.4:
/// "Construct aggregates through builders that supply valid defaults, so each test
/// specifies only the values relevant to what it verifies." A fixed clock
/// (<see cref="NowUtc"/>), never <c>DateTimeOffset.UtcNow</c>, per that same document's
/// own 2.1 ("must not touch... a clock").
/// </summary>
internal static class TestData
{
    public static readonly DateTimeOffset NowUtc = new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

    public static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static readonly Guid RecipientUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public static List<NotificationChannel> NewSupportedChannels() => [NotificationChannel.Email, NotificationChannel.InApp];

    public static NotificationTemplate NewTemplate(
        Guid? tenantId = null,
        string templateKey = "leave.approved",
        NotificationType notificationType = NotificationType.ApprovalResult,
        DateTimeOffset? nowUtc = null) =>
        NotificationTemplate.Create(
            tenantId ?? TenantId, templateKey, notificationType, "en-US", "Your leave request was approved",
            "Hi {{EmployeeName}}, your leave request has been approved.", NewSupportedChannels(), nowUtc ?? NowUtc).Value;

    public static NotificationTemplate PublishedTemplate(Guid? tenantId = null, DateTimeOffset? nowUtc = null)
    {
        var template = NewTemplate(tenantId, nowUtc: nowUtc);
        template.PublishVersion(1, nowUtc ?? NowUtc);
        return template;
    }

    public static NotificationEntity NewNotification(
        Guid? tenantId = null,
        Guid? recipientUserId = null,
        NotificationType notificationType = NotificationType.ApprovalResult,
        NotificationChannel channel = NotificationChannel.InApp,
        DateTimeOffset? nowUtc = null) =>
        NotificationEntity.Request(
            tenantId ?? TenantId, recipientUserId ?? RecipientUserId, notificationType, channel, "leave.approved",
            "Your leave request was approved", "Hi Employee, your leave request has been approved.", nowUtc ?? NowUtc).Value;

    public static NotificationEntity QueuedNotification(Guid? tenantId = null, Guid? recipientUserId = null, DateTimeOffset? nowUtc = null)
    {
        var notification = NewNotification(tenantId, recipientUserId, nowUtc: nowUtc);
        notification.Queue(nowUtc ?? NowUtc);
        return notification;
    }

    public static NotificationEntity ProcessingNotification(Guid? tenantId = null, Guid? recipientUserId = null, DateTimeOffset? nowUtc = null)
    {
        var notification = QueuedNotification(tenantId, recipientUserId, nowUtc);
        notification.StartProcessing();
        return notification;
    }

    public static NotificationEntity SentNotification(Guid? tenantId = null, Guid? recipientUserId = null, DateTimeOffset? nowUtc = null)
    {
        var notification = ProcessingNotification(tenantId, recipientUserId, nowUtc);
        notification.MarkSent(nowUtc ?? NowUtc);
        return notification;
    }

    public static NotificationEntity DeliveredNotification(Guid? tenantId = null, Guid? recipientUserId = null, DateTimeOffset? nowUtc = null)
    {
        var notification = SentNotification(tenantId, recipientUserId, nowUtc);
        notification.MarkDelivered(nowUtc ?? NowUtc);
        return notification;
    }

    public static NotificationPreference NewPreference(Guid? tenantId = null, Guid? userId = null, DateTimeOffset? nowUtc = null) =>
        NotificationPreference.Register(
            tenantId ?? TenantId, userId ?? RecipientUserId, "en-US", NewSupportedChannels(),
            TimeSpan.FromHours(22), TimeSpan.FromHours(7), false, false, nowUtc ?? NowUtc).Value;
}
