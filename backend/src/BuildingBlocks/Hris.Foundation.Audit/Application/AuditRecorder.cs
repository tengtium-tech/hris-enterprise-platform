using System.Text.Json;
using Hris.Foundation.Audit.Domain;
using Hris.Foundation.Events.Domain;
using Hris.Foundation.Identity.Domain;
using Hris.SharedKernel;

namespace Hris.Foundation.Audit.Application;

/// <summary>
/// The one implementation of <see cref="IAuditRecorder"/>. Persists the
/// <see cref="AuditRecord"/> itself via <see cref="IAuditRecordRepository"/>, then
/// publishes <see cref="AuditRecordCreated"/> through Event Framework's own
/// <see cref="IEventPublisher"/> -- both calls add to the same, already-open
/// <c>HrisDbContext</c> without calling <c>SaveChangesAsync</c> themselves (see this
/// interface's own remarks for why), so the caller's own <c>TransactionBehavior</c>
/// persists the business change, the audit record, and the queued event together, in
/// one commit.
///
/// Publishing on every single audit write is not the same performance concern
/// <c>CheckAuthorizationQueryHandler</c>'s own remarks raise for authorization checks:
/// an authorization check by itself performs no persistence at all today, so adding
/// an event write there would be a new database write on every evaluation. Recording
/// an audit entry already IS a database write -- adding one more owned row
/// (the outbox entry) to a save that is happening regardless is a marginal cost, not
/// a new one, and audit-framework.md's own Domain Events section names
/// <see cref="AuditRecordCreated"/> as a real, intended capability.
/// </summary>
internal sealed class AuditRecorder : IAuditRecorder
{
    private const string _sourceModule = "Audit";

    private readonly IAuditRecordRepository _repository;
    private readonly IEventPublisher _eventPublisher;
    private readonly TimeProvider _timeProvider;

    public AuditRecorder(IAuditRecordRepository repository, IEventPublisher eventPublisher, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _eventPublisher = Guard.AgainstNull(eventPublisher, nameof(eventPublisher));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task RecordAsync(
        AuditCategory category,
        string action,
        string businessEntity,
        string entityIdentifier,
        string sourceSystem,
        AuditResult outcome,
        Guid tenantId,
        Guid? actorId = null,
        string? previousValue = null,
        string? newValue = null,
        string? clientApplication = null,
        string? ipAddress = null,
        string? deviceInformation = null,
        Guid? correlationId = null,
        IReadOnlyDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        var nowUtc = _timeProvider.GetUtcNow();

        CorrelationId? recordCorrelationId = null;
        if (correlationId.HasValue)
        {
            var correlationIdResult = CorrelationId.Create(correlationId.Value);
            if (correlationIdResult.IsFailure)
            {
                throw new ArgumentException(correlationIdResult.Error.Description, nameof(correlationId));
            }

            recordCorrelationId = correlationIdResult.Value;
        }

        var recordResult = AuditRecord.Create(
            nowUtc,
            actorId.HasValue ? new UserAccountId(actorId.Value) : null,
            category,
            action,
            businessEntity,
            entityIdentifier,
            sourceSystem,
            outcome,
            previousValue,
            newValue,
            clientApplication,
            ipAddress,
            deviceInformation,
            recordCorrelationId,
            metadata);

        if (recordResult.IsFailure)
        {
            // A caller contract violation (missing action/businessEntity/entityIdentifier/
            // sourceSystem), not a business outcome -- this interface has no Result
            // channel to return through, the same translation ILoggingService.LogAsync
            // documents for its own Result-to-exception boundary.
            throw new ArgumentException(recordResult.Error.Description);
        }

        var record = recordResult.Value;
        await _repository.AddAsync(record, cancellationToken).ConfigureAwait(false);

        var domainEvent = new AuditRecordCreated(Guid.NewGuid(), nowUtc, record.Id, category, businessEntity, entityIdentifier);
        var envelopeCorrelationId = recordCorrelationId ?? CorrelationId.NewId();

        var envelopeResult = EventEnvelope.Create(
            domainEvent,
            _sourceModule,
            EventCategory.DomainEvent,
            envelopeCorrelationId,
            JsonSerializer.Serialize(domainEvent),
            tenantId,
            actor: actorId.HasValue ? new UserAccountId(actorId.Value) : null);

        if (envelopeResult.IsFailure)
        {
            throw new ArgumentException(envelopeResult.Error.Description, nameof(tenantId));
        }

        await _eventPublisher.PublishAsync([envelopeResult.Value], cancellationToken).ConfigureAwait(false);
    }
}
