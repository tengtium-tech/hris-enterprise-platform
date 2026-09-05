namespace Hris.Foundation.Notification.Domain;

/// <summary>
/// The union of every status notification-framework.md names for one notification's own
/// progress, across two sections written from different angles: Notification Lifecycle
/// ("Created -&gt; Queued -&gt; Scheduled -&gt; Processing -&gt; Delivered," alternatives
/// "Failed, Expired, Cancelled, Suppressed") and Delivery Tracking ("Created, Queued,
/// Sent, Delivered, Read, Acknowledged, Failed"). Both describe the same one entity's
/// own journey, not two different entities -- <see cref="Sent"/>, <see cref="Read"/>,
/// and <see cref="Acknowledged"/> are Delivery Tracking's own additions to the Lifecycle
/// diagram's own stages. <see cref="DeadLetter"/> is added for the Retry Processing
/// section's own named "Dead Letter Queue (DLQ)" mechanism -- exhausted retries land
/// here, the identical terminal-after-retries-exhausted state
/// <c>JobStatus.DeadLetter</c> already establishes for its own framework.
/// </summary>
public enum NotificationStatus
{
    Created = 0,
    Queued = 1,
    Scheduled = 2,
    Processing = 3,
    Sent = 4,
    Delivered = 5,
    Read = 6,
    Acknowledged = 7,
    Failed = 8,
    Expired = 9,
    Cancelled = 10,
    Suppressed = 11,
    DeadLetter = 12,
}
