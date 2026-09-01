using Hris.SharedKernel;

namespace Hris.Foundation.Events.Domain;

/// <summary>
/// The persisted outbox row outbox-pattern.md's Outbox Table section describes ("Event
/// identifier, Event type, Event payload, Aggregate identifier, Occurred timestamp,
/// Published timestamp (nullable), Processing status, Retry count, Correlation
/// identifier") and event-framework.md's own Transactional Publication section calls
/// for ("The event is written to an outbox table within the same database transaction
/// as the business change"). <see cref="EventEnvelope"/>'s own remarks explain why it
/// is not this Aggregate Root itself -- "the outbox dispatcher's own bookkeeping over a
/// persisted queue... is explicitly an Infrastructure/persistence concern" -- but
/// dbcontext-design.md's "Only Aggregate Roots are persisted directly" still requires
/// some Aggregate Root to exist for EF Core to map; this is that root,
/// <see cref="Envelope"/> is the immutable payload it wraps.
///
/// Deliberately a mechanical status/retry-count tracker, not a rich business Aggregate
/// the way <c>ConfigurationSetting</c> or <c>UserAccount</c> are -- there is no business
/// invariant here beyond "a dispatched entry has a dispatch timestamp" and "a
/// dead-lettered entry has exhausted its attempts." Every transition method is
/// idempotent on retry (matching this framework's own "Idempotent Processing"
/// principle, since the background dispatcher that calls these may itself be retried
/// after a crash).
/// </summary>
public sealed class OutboxEntry : AggregateRoot<OutboxEntryId>
{
    // null! -- valid for every caller of the public API: the application-facing
    // constructor above always assigns a real value; only the EF-only constructor
    // (see its own remarks) leaves this to be populated post-construction, which the
    // compiler's definite-assignment analysis cannot see coming.
    public EventEnvelope Envelope { get; } = null!;

    public OutboxEntryStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; }

    public DateTimeOffset? DispatchedAtUtc { get; private set; }

    public int AttemptCount { get; private set; }

    public DateTimeOffset? LastAttemptAtUtc { get; private set; }

    public string? LastFailureReason { get; private set; }

    private OutboxEntry(OutboxEntryId id, EventEnvelope envelope, DateTimeOffset createdAtUtc)
        : base(id)
    {
        Envelope = envelope;
        Status = OutboxEntryStatus.Pending;
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>
    /// EF Core materialization only -- never called by application code, which always
    /// goes through the constructor above via <see cref="Create"/>. EF Core's own
    /// constructor-binding convention cannot bind <c>envelope</c> through a
    /// constructor parameter because <see cref="Envelope"/> is an owned-type
    /// navigation (<c>OutboxEntryConfiguration</c>'s own <c>OwnsOne</c>), not a scalar
    /// or converted property ("navigations to related entities, including references
    /// to owned types, cannot be bound," per that convention's own documented rule);
    /// every other parameter above binds fine. Rather than change the constructor
    /// application code actually calls, this second constructor gives EF Core one
    /// that satisfies its own binding rule, and it selects this one automatically for
    /// materialization, since it is the best fully-bindable match. EF Core then
    /// populates <see cref="Envelope"/> through this get-only auto-property's own
    /// compiler-generated backing field, the same as any owned reference with no
    /// custom getter body -- see <c>ConfigurationSetting</c>'s own identical second
    /// constructor for the full reasoning.
    /// </summary>
    private OutboxEntry(OutboxEntryId id, DateTimeOffset createdAtUtc)
        : base(id)
    {
        Status = OutboxEntryStatus.Pending;
        CreatedAtUtc = createdAtUtc;
    }

    public static Result<OutboxEntry> Create(EventEnvelope envelope, DateTimeOffset nowUtc)
    {
        Guard.AgainstNull(envelope, nameof(envelope));

        return Result.Success(new OutboxEntry(new OutboxEntryId(Guid.NewGuid()), envelope, nowUtc));
    }

    /// <summary>
    /// Idempotent: a dispatcher that crashes after marking dispatched but before its
    /// own commit, then retries the same entry on restart, must not fail the second
    /// time -- the identical reasoning <c>Session.Revoke</c>'s own remarks give for
    /// <c>UserAccount.RevokeSession</c>'s idempotence.
    /// </summary>
    public Result MarkDispatched(DateTimeOffset nowUtc)
    {
        if (Status == OutboxEntryStatus.Dispatched)
        {
            return Result.Success();
        }

        Status = OutboxEntryStatus.Dispatched;
        DispatchedAtUtc = nowUtc;
        return Result.Success();
    }

    /// <summary>
    /// event-framework.md's Retry Processing section ("Automatic Retry... Retry
    /// Limits") and Dead Letter Queue section ("Events that cannot be processed should
    /// be routed to a Dead Letter Queue"). <paramref name="maxAttempts"/> is supplied
    /// by the caller rather than a constant here, so the Infrastructure-layer
    /// dispatcher can source it from Configuration Framework the same way
    /// <c>AuthenticateCommandHandler</c> sources its own policy values, without this
    /// Domain method depending on Configuration Framework itself (`CTR-ARC-002`).
    /// </summary>
    public Result RecordFailedAttempt(string reason, DateTimeOffset nowUtc, int maxAttempts)
    {
        if (Status == OutboxEntryStatus.Dispatched)
        {
            return Result.Failure(EventErrors.OutboxEntryAlreadyDispatched);
        }

        AttemptCount++;
        LastAttemptAtUtc = nowUtc;
        LastFailureReason = Guard.AgainstNullOrWhiteSpace(reason, nameof(reason));
        Status = AttemptCount >= maxAttempts ? OutboxEntryStatus.DeadLettered : OutboxEntryStatus.Failed;
        return Result.Success();
    }

    /// <summary>
    /// The single underlying operation behind both of this framework's own manual-
    /// recovery Client-Facing... Application-layer commands: Dead Letter Queue's own
    /// "Manual Recovery" (requeuing a <see cref="OutboxEntryStatus.DeadLettered"/>
    /// entry) and Event Replay ("Replaying historical events for: Recovery, Testing,
    /// Data Synchronization, Analytics, Audit" -- requeuing an already-
    /// <see cref="OutboxEntryStatus.Dispatched"/> entry on purpose). Each Application
    /// command enforces its own narrower precondition on which starting
    /// <see cref="Status"/> is acceptable; this method itself only enforces the one
    /// invariant common to both: a requeue always resets <see cref="AttemptCount"/> to
    /// zero, the same "give a clean slate" reasoning <c>UserAccount.Unlock</c> already
    /// applies to <c>FailedAuthenticationAttemptCount</c> -- a manually requeued entry
    /// should not immediately re-dead-letter itself off a stale count.
    /// </summary>
    public Result Requeue(DateTimeOffset nowUtc)
    {
        if (Status == OutboxEntryStatus.Pending)
        {
            return Result.Success();
        }

        Status = OutboxEntryStatus.Pending;
        DispatchedAtUtc = null;
        AttemptCount = 0;
        LastAttemptAtUtc = nowUtc;
        LastFailureReason = null;
        return Result.Success();
    }
}
