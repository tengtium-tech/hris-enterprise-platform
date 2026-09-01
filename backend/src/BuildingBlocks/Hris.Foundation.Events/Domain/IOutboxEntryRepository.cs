namespace Hris.Foundation.Events.Domain;

/// <summary>
/// Persistence abstraction for the <see cref="OutboxEntry"/> Aggregate Root, per
/// repositories.md's "interface in the Domain layer... implementation in
/// Infrastructure" split -- the identical shape <c>IUserAccountRepository</c>
/// and <c>IConfigurationSettingRepository</c> already establish.
/// </summary>
public interface IOutboxEntryRepository
{
    Task<OutboxEntry?> GetByIdAsync(OutboxEntryId id, CancellationToken cancellationToken);

    /// <summary>
    /// The Infrastructure-layer background dispatcher's own read query -- ordered by
    /// <see cref="EventEnvelope.OccurredOnUtc"/> ascending, per this framework's own
    /// Event Ordering section: "Within a single Aggregate, events should normally be
    /// published in creation order" (outbox-pattern.md's own phrasing). A best-effort
    /// approximation of per-aggregate ordering, not a guarantee across aggregates --
    /// that document's own "Global ordering across unrelated Aggregates is not
    /// required."
    /// </summary>
    Task<IReadOnlyList<OutboxEntry>> GetPendingBatchAsync(int batchSize, CancellationToken cancellationToken);

    /// <summary>
    /// Dead Letter Queue's own "DLQ Monitoring" capability. <paramref name="tenantId"/>
    /// is optional -- DLQ monitoring is an operator/platform capability
    /// (`../00-project/platform-operations-roles.md`), not a tenant self-service one,
    /// so a <c>null</c> value intentionally returns entries across every tenant. Once
    /// Authorization Framework exists, gating this query to a platform-operator
    /// permission is that framework's own concern, not this repository's.
    /// </summary>
    Task<IReadOnlyList<OutboxEntry>> GetDeadLetteredAsync(Guid? tenantId, int maxResults, CancellationToken cancellationToken);

    Task AddAsync(OutboxEntry entry, CancellationToken cancellationToken);
}
