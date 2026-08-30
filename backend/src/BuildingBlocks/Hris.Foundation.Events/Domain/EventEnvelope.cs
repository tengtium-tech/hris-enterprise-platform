using Hris.Foundation.Identity.Domain;
using Hris.SharedKernel;

namespace Hris.Foundation.Events.Domain;

/// <summary>
/// Wraps one already-raised <see cref="IDomainEvent"/> with the fields
/// event-framework.md's Event Structure section requires for dispatch: "Event
/// Identifier, Event Type, Event Version, Timestamp, Source Module, Correlation
/// Identifier, Tenant Identifier, Company Identifier, Actor, Payload, Metadata."
///
/// Deliberately not an Aggregate Root with its own lifecycle-transition methods the
/// way <c>ConfigurationSetting</c> or <c>UserAccount</c> are: the Event Lifecycle
/// this document names ("Created -&gt; Validated -&gt; Published -&gt; Delivered
/// -&gt; Processed -&gt; Archived," with the parallel "Retry -&gt; Dead Letter Queue
/// -&gt; Manual Recovery" path) is the outbox dispatcher's own bookkeeping over a
/// persisted queue -- explicitly an Infrastructure/persistence concern this
/// document itself defers ("The persistence model is specified in
/// ../02-architecture/05-data-architecture/outbox-pattern.md"), not a business
/// workflow a caller drives through Domain methods. This type is the immutable
/// record the dispatcher moves through that lifecycle, not a participant in it.
///
/// <see cref="Payload"/> is the already-serialized <see cref="IDomainEvent"/> rather
/// than the live object -- outbox persistence, the whole reason this framework
/// exists, requires a byte-stable form, and serialization is an Infrastructure
/// concern (`CTR-ARC-001`); this type only carries the *contract* for what the
/// serialized form must be accompanied by, not the serializer itself.
/// </summary>
public sealed class EventEnvelope : ValueObject
{
    public Guid EventId { get; }

    public string EventType { get; }

    public int EventVersion { get; }

    public DateTimeOffset OccurredOnUtc { get; }

    public string SourceModule { get; }

    public EventCategory Category { get; }

    public CorrelationId CorrelationId { get; }

    public Guid? TenantId { get; }

    public Guid? CompanyId { get; }

    public UserAccountId? Actor { get; }

    public string Payload { get; }

    public IReadOnlyDictionary<string, string> Metadata { get; }

    private EventEnvelope(
        Guid eventId,
        string eventType,
        int eventVersion,
        DateTimeOffset occurredOnUtc,
        string sourceModule,
        EventCategory category,
        CorrelationId correlationId,
        Guid? tenantId,
        Guid? companyId,
        UserAccountId? actor,
        string payload,
        IReadOnlyDictionary<string, string> metadata)
    {
        EventId = eventId;
        EventType = eventType;
        EventVersion = eventVersion;
        OccurredOnUtc = occurredOnUtc;
        SourceModule = sourceModule;
        Category = category;
        CorrelationId = correlationId;
        TenantId = tenantId;
        CompanyId = companyId;
        Actor = actor;
        Payload = payload;
        Metadata = metadata;
    }

    public static Result<EventEnvelope> Create(
        IDomainEvent domainEvent,
        string? sourceModule,
        EventCategory category,
        CorrelationId correlationId,
        string payload,
        Guid? tenantId,
        Guid? companyId = null,
        UserAccountId? actor = null,
        int eventVersion = 1,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        Guard.AgainstNull(domainEvent, nameof(domainEvent));
        Guard.AgainstNull(correlationId, nameof(correlationId));
        Guard.AgainstNullOrEmpty(payload, nameof(payload));

        if (string.IsNullOrWhiteSpace(sourceModule))
        {
            return Result.Failure<EventEnvelope>(EventErrors.SourceModuleRequired);
        }

        if (tenantId is null && category is EventCategory.DomainEvent or EventCategory.IntegrationEvent)
        {
            return Result.Failure<EventEnvelope>(EventErrors.TenantIdRequiredForScopedEvent);
        }

        return Result.Success(new EventEnvelope(
            domainEvent.EventId,
            domainEvent.GetType().Name,
            eventVersion,
            domainEvent.OccurredOnUtc,
            sourceModule.Trim(),
            category,
            correlationId,
            tenantId,
            companyId,
            actor,
            payload,
            metadata ?? new Dictionary<string, string>()));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return EventId;
        yield return EventVersion;
    }
}
