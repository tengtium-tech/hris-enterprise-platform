namespace Hris.Foundation.Notification.Domain;

/// <summary>
/// notification-framework.md's own Notification Types section, verbatim order.
/// Organizations may define custom types per that section's own closing sentence, but
/// this Sprint's own build supports only the document's own named set -- a genuine
/// per-tenant extensible-type registry is exactly the kind of concrete Configuration
/// Framework integration point <c>DependencyInjection.cs</c>'s own remarks describe as
/// deferred, not built here.
/// </summary>
public enum NotificationType
{
    Information = 0,
    Warning = 1,
    Reminder = 2,
    ApprovalRequest = 3,
    ApprovalResult = 4,
    Alert = 5,
    Announcement = 6,
    Error = 7,
    Success = 8,
}
