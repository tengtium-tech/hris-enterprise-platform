namespace Hris.Foundation.Events.Domain;

/// <summary>
/// The processing status outbox-pattern.md's Outbox Table section names as a required
/// field ("Processing status"). A deliberately simpler model than event-framework.md's
/// own six-stage Event Lifecycle diagram ("Created -&gt; Validated -&gt; Published -&gt;
/// Delivered -&gt; Processed -&gt; Archived") -- that diagram describes the event's own
/// conceptual journey; outbox-pattern.md, the document event-framework.md itself
/// defers the persistence model to, describes a simpler status/retry-count/published-
/// timestamp shape ("unpublished events... mark published... retry failed"), which
/// this enum follows directly. <see cref="Failed"/> and <see cref="DeadLettered"/>
/// together are this outbox's own answer to event-framework.md's alternative-outcomes
/// path ("Retry -&gt; Dead Letter Queue -&gt; Manual Recovery").
/// </summary>
public enum OutboxEntryStatus
{
    Pending = 0,
    Dispatched,
    Failed,
    DeadLettered,
}
