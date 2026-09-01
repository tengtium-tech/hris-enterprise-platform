using Hris.Foundation.Events.Domain;
using Hris.SharedKernel;

namespace Hris.Foundation.Events.Infrastructure.Publishing;

/// <summary>
/// The one implementation of <see cref="IEventPublisher"/>, per that interface's own
/// remarks: "The intended caller is the Application layer's own transaction/Unit-of-
/// Work boundary... after a business change and its aggregate's accumulated
/// DomainEvents both commit successfully, that boundary calls PublishAsync with the
/// same events, inside the same transaction."
///
/// Concretely, "the same transaction" falls out of this platform's existing shared
/// scaffolding without any change to it: <c>HrisDbContext</c> is Scoped, so a command
/// handler's own repository (e.g. <c>ConfigurationSettingRepository</c>) and this
/// publisher's own <see cref="IOutboxEntryRepository"/> resolve the identical DbContext
/// instance within one request; <c>TransactionBehavior</c> calls
/// <c>SaveChangesAsync</c> exactly once, after the handler returns success, covering
/// both the aggregate's own persisted change and every <see cref="OutboxEntry"/> added
/// here. A command handler therefore calls <see cref="PublishAsync"/> directly, inline,
/// before returning its own <c>Result.Success</c> -- there is no automatic,
/// reflection-based dispatch that scans every tracked aggregate's own DomainEvents for
/// every command platform-wide. Building that would require a request-scoped tenant
/// context accessor (to populate <see cref="EventEnvelope.TenantId"/> for an arbitrary
/// event whose own record may not carry one -- most existing Domain Event records do
/// not) that does not exist yet in this Sprint, the same class of gap
/// <c>LoggingService</c>'s own remarks already document for Identity/Authorization
/// integration. This publisher's own capability is real and usable today by any caller
/// that, like every existing command handler, already has its own tenant and
/// correlation context in hand.
///
/// Does not call <c>IUnitOfWork.SaveChangesAsync</c> itself -- see the remarks
/// above; that call belongs to the calling command's own <c>TransactionBehavior</c>
/// pass, not to this publisher.
/// </summary>
internal sealed class OutboxEventPublisher : IEventPublisher
{
    private readonly IOutboxEntryRepository _repository;
    private readonly TimeProvider _timeProvider;

    public OutboxEventPublisher(IOutboxEntryRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task PublishAsync(IReadOnlyCollection<EventEnvelope> envelopes, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(envelopes, nameof(envelopes));

        var nowUtc = _timeProvider.GetUtcNow();

        foreach (var envelope in envelopes)
        {
            var entryResult = OutboxEntry.Create(envelope, nowUtc);
            if (entryResult.IsFailure)
            {
                // Only reachable if envelope were null, which Guard.AgainstNull above
                // already rules out for the collection itself -- an individual null
                // element would still throw from OutboxEntry.Create's own Guard, the
                // same "caller contract violation, not a business outcome" reasoning
                // LoggingService.LogAsync documents for its own Result-to-exception
                // translation, since this interface has no Result channel to return
                // through (IEventPublisher.PublishAsync returns a plain Task).
                throw new InvalidOperationException(entryResult.Error.Description);
            }

            await _repository.AddAsync(entryResult.Value, cancellationToken).ConfigureAwait(false);
        }
    }
}
