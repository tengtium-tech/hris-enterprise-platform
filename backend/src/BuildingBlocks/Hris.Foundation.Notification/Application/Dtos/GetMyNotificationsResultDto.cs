namespace Hris.Foundation.Notification.Application.Dtos;

/// <summary>
/// The read-side shape <c>GetMyNotificationsQuery</c> returns, per notification-framework.md's
/// own wording: "the unread count... is derived from this same result, not a separate
/// query."
/// </summary>
public sealed record GetMyNotificationsResultDto(
    IReadOnlyList<NotificationDto> Items,
    int TotalCount,
    int UnreadCount);
