namespace Hris.Foundation.Notification.Domain;

/// <summary>
/// notification-framework.md's own Delivery Channels section, verbatim order. One
/// <see cref="Domain.Notification"/> row is always exactly one recipient on exactly one
/// channel -- "Record delivery status per recipient and per channel for audit" (AI
/// Implementation Guidance) is satisfied by that row shape directly, not by a separate
/// fan-out parent aggregate the document never names.
/// </summary>
public enum NotificationChannel
{
    Email = 0,
    Sms = 1,
    MobilePush = 2,
    InApp = 3,
    Browser = 4,
    MicrosoftTeams = 5,
    Slack = 6,
    Webhook = 7,
}
