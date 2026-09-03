using Hris.SharedKernel;

namespace Hris.Foundation.Numbering.Domain;

/// <summary>
/// Aggregate Root holding one Number Series' own configuration -- how identifiers in
/// this series are formatted and how its own running sequence resets. Source:
/// docs/03-foundation/numbering-framework.md, Core Concepts ("A Number Series defines
/// how identifiers are generated... Each series is independently configurable").
///
/// Fourth framework built in Sprint 4. This aggregate owns <see cref="CurrentSequenceValue"/>
/// as its own durable counter, but <see cref="IIssuedNumberRepository"/>'s own atomic
/// increment (never this aggregate's own in-memory mutation) is the only path a caller
/// uses to actually advance it -- see that repository interface's own remarks for why:
/// the AI Implementation Guidance's "two simultaneous requests must never receive the
/// same number" (CTR-DAT-001) is a real-concurrency guarantee ordinary EF Core change
/// tracking (load, mutate a field, save) cannot provide on its own, since two concurrent
/// requests loading the same row would both compute the same "next" value from the same
/// stale read. <see cref="ResetSequence"/> is the one method that legitimately mutates
/// <see cref="CurrentSequenceValue"/> through this aggregate directly, since a reset is
/// a deliberate, single-actor administrative act, not a hot concurrent path.
/// </summary>
public sealed class NumberSeries : AggregateRoot<NumberSeriesId>
{
    public SeriesKey Key { get; }

    public NumberPrefix Prefix { get; private set; }

    public NumberFormat Format { get; private set; }

    public SequenceResetPolicy ResetPolicy { get; private set; }

    public long CurrentSequenceValue { get; private set; }

    public DateTimeOffset? LastResetAtUtc { get; private set; }

    private NumberSeries(NumberSeriesId id, SeriesKey key, NumberPrefix prefix, NumberFormat format, SequenceResetPolicy resetPolicy)
        : base(id)
    {
        Key = key;
        Prefix = prefix;
        Format = format;
        ResetPolicy = resetPolicy;
        CurrentSequenceValue = 0;
    }

    /// <summary>
    /// EF Core materialization only -- never called by application code, which always
    /// goes through the constructor above via <see cref="Register"/>. EF Core's own
    /// constructor-binding convention cannot bind <paramref name="format"/>'s own
    /// parameter, the identical "navigations to related entities, including references
    /// to owned types, cannot be bound" limitation Sprint 3's own EF Core Persistence
    /// Pitfalls finding documents -- confirmed here to extend to a Complex Type
    /// (<see cref="NumberFormat"/>) exactly as it does to an owned type, found
    /// empirically by this same scratch model-build harness, not assumed. Every other
    /// parameter binds fine; this second constructor gives EF Core one that satisfies
    /// its own binding rule, and it selects this one automatically for materialization.
    /// EF Core then populates <see cref="Format"/> the normal way it populates every
    /// other private-set property here that was never a constructor parameter to begin
    /// with (<see cref="ResetPolicy"/>, <see cref="CurrentSequenceValue"/>,
    /// <see cref="LastResetAtUtc"/>).
    /// </summary>
    private NumberSeries(NumberSeriesId id, SeriesKey key, NumberPrefix prefix, SequenceResetPolicy resetPolicy)
        : base(id)
    {
        Key = key;
        Prefix = prefix;
        Format = null!;
        ResetPolicy = resetPolicy;
        CurrentSequenceValue = 0;
    }

    /// <summary>
    /// Registers a new series. Global key uniqueness is checked by the caller before
    /// this factory runs (<see cref="INumberSeriesRepository.ExistsByKeyAsync"/>), not
    /// here -- the same split every other framework's own uniqueness-checked factory in
    /// this codebase establishes.
    /// </summary>
    public static Result<NumberSeries> Register(
        SeriesKey key, NumberPrefix prefix, NumberFormat format, SequenceResetPolicy resetPolicy)
    {
        Guard.AgainstNull(key, nameof(key));
        Guard.AgainstNull(prefix, nameof(prefix));
        Guard.AgainstNull(format, nameof(format));

        return Result.Success(new NumberSeries(new NumberSeriesId(Guid.NewGuid()), key, prefix, format, resetPolicy));
    }

    public Result UpdateFormat(NumberPrefix prefix, NumberFormat format, SequenceResetPolicy resetPolicy, DateTimeOffset nowUtc)
    {
        Guard.AgainstNull(prefix, nameof(prefix));
        Guard.AgainstNull(format, nameof(format));

        Prefix = prefix;
        Format = format;
        ResetPolicy = resetPolicy;

        AddDomainEvent(new NumberSeriesUpdated(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    /// <summary>
    /// An explicit, administrator-triggered reset -- deliberately not something
    /// <see cref="IIssuedNumberRepository"/>'s own atomic increment detects and applies
    /// automatically mid-request. An implementation that checked "has the reset
    /// boundary passed" inside the same hot path that increments the sequence invites a
    /// race between two concurrent requests that both observe the boundary crossed and
    /// both attempt to reset, or one that reads the old sequence a moment after another
    /// has already reset it -- a single, explicit, administrative actor calling this
    /// method (on a schedule, if desired, via a background job outside this Sprint's
    /// own scope) avoids that class of defect entirely rather than working around it.
    /// Valid regardless of <see cref="ResetPolicy"/> -- the policy governs whether a
    /// reset happens *automatically*, not whether an administrator may force one.
    /// </summary>
    public Result ResetSequence(DateTimeOffset nowUtc)
    {
        CurrentSequenceValue = 0;
        LastResetAtUtc = nowUtc;

        AddDomainEvent(new SequenceReset(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    /// <summary>
    /// Reconciles this in-memory aggregate's own <see cref="CurrentSequenceValue"/>
    /// after <see cref="IIssuedNumberRepository"/>'s own atomic increment has already
    /// advanced the durable value out-of-band -- so a caller that re-reads this same
    /// aggregate within the same unit of work sees the value it just consumed, not a
    /// stale one. Never itself performs the increment; it only records one that has
    /// already happened.
    /// </summary>
    internal void ReconcileSequenceValueAfterAtomicIncrement(long newSequenceValue)
    {
        CurrentSequenceValue = newSequenceValue;
    }
}
