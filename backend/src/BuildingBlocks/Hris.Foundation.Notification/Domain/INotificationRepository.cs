namespace Hris.Foundation.Notification.Domain;

/// <summary>
/// Repository interface in the Domain layer, implementation in Infrastructure, per
/// repositories.md's own split.
/// </summary>
public interface INotificationRepository
{
    Task<Notification?> GetByIdAsync(NotificationId id, CancellationToken cancellationToken);

    /// <summary>
    /// Backs <c>GetMyNotificationsQuery</c> -- notification-framework.md's own
    /// Client-Facing Commands and Queries section: "The caller's own in-app
    /// notifications, filterable by Read/unread, paged; the unread count... is derived
    /// from this same result, not a separate query." Scoped to
    /// <see cref="NotificationChannel.InApp"/> and <paramref name="recipientUserId"/>
    /// within <paramref name="tenantId"/> unconditionally, per CTR-ISO-004 and this
    /// document's own "no admin-override path" requirement. Returns the unread count
    /// across every one of the caller's own in-app notifications regardless of
    /// <paramref name="isRead"/> or paging, so a caller can render a badge count
    /// alongside a filtered, paged list from one round trip, matching this document's
    /// own "derived from this same result" wording.
    /// </summary>
    Task<(IReadOnlyList<Notification> Items, int TotalCount, int UnreadCount)> ListInAppForRecipientAsync(
        Guid recipientUserId, Guid tenantId, bool? isRead, int skip, int take, CancellationToken cancellationToken);

    Task AddAsync(Notification notification, CancellationToken cancellationToken);
}
