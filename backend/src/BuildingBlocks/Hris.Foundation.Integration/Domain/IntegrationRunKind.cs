namespace Hris.Foundation.Integration.Domain;

/// <summary>
/// Which of the document's own three distinct event pairs an <see cref="IntegrationRun"/>
/// raises on <see cref="IntegrationRun.Start"/>/<see cref="IntegrationRun.Complete"/> --
/// see <see cref="IntegrationEvents"/>'s own remarks for the full mapping. Source:
/// docs/03-foundation/integration-framework.md distinguishes an ad-hoc API call
/// (Domain Events: <c>IntegrationStarted</c>/<c>IntegrationCompleted</c>), a
/// Synchronization run (<c>SynchronizationStarted</c>/<c>SynchronizationCompleted</c>,
/// per the document's own Synchronization section), and an outbound Webhook delivery
/// (<c>WebhookDelivered</c>, per the document's own Webhooks section) as three
/// separate concepts that nonetheless share the identical Started/Completed/Failed/
/// Retry state machine below.
/// </summary>
public enum IntegrationRunKind
{
    IntegrationCall = 0,
    Synchronization,
    WebhookDelivery,
}
