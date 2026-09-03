using Hris.SharedKernel;

namespace Hris.Foundation.Numbering.Domain;

/// <summary>
/// Aggregate Root for one specific identifier's own journey through
/// numbering-framework.md's own Number Lifecycle. See <see cref="IssuedNumberId"/>'s
/// own remarks for why this is a separate Aggregate Root from <see cref="NumberSeries"/>,
/// not a child Entity of it.
/// </summary>
public sealed class IssuedNumber : AggregateRoot<IssuedNumberId>
{
    public NumberSeriesId NumberSeriesId { get; }

    public long? SequenceValue { get; private set; }

    public FormattedNumber? FormattedNumber { get; private set; }

    public NumberLifecycleStatus Status { get; private set; }

    public string? AssignedToType { get; private set; }

    public string? AssignedToReferenceId { get; private set; }

    public DateTimeOffset RequestedAtUtc { get; }

    public DateTimeOffset? IssuedAtUtc { get; private set; }

    private IssuedNumber(IssuedNumberId id, NumberSeriesId numberSeriesId, DateTimeOffset requestedAtUtc)
        : base(id)
    {
        NumberSeriesId = numberSeriesId;
        RequestedAtUtc = requestedAtUtc;
        Status = NumberLifecycleStatus.Requested;
    }

    /// <summary>
    /// Begins a new identifier's lifecycle, in <see cref="NumberLifecycleStatus.Requested"/>
    /// -- no sequence value is consumed yet, matching the AI Implementation Guidance's
    /// "never trust client-supplied metadata" spirit applied to sequence values too: a
    /// number is not real until <see cref="Reserve"/> actually claims one atomically.
    /// </summary>
    public static Result<IssuedNumber> Request(NumberSeriesId numberSeriesId, DateTimeOffset nowUtc)
    {
        var issuedNumber = new IssuedNumber(new IssuedNumberId(Guid.NewGuid()), numberSeriesId, nowUtc);

        issuedNumber.AddDomainEvent(new NumberRequested(Guid.NewGuid(), nowUtc, issuedNumber.Id, numberSeriesId));
        return Result.Success(issuedNumber);
    }

    /// <summary>
    /// Claims a real, already-atomically-incremented sequence value. Per
    /// <see cref="IIssuedNumberRepository"/>'s own remarks, <paramref name="sequenceValue"/>
    /// must already have been produced by <see cref="INumberSeriesRepository.IncrementAndGetNextSequenceValueAsync"/>
    /// before this method is called -- this method only records a claim already made,
    /// never performs the claim itself, so this aggregate's own state never depends on
    /// EF Core's change-tracking save order to be correct under concurrency.
    /// </summary>
    public Result Reserve(long sequenceValue, FormattedNumber formattedNumber, DateTimeOffset nowUtc)
    {
        Guard.AgainstNull(formattedNumber, nameof(formattedNumber));

        if (Status != NumberLifecycleStatus.Requested)
        {
            return Result.Failure(NumberingErrors.InvalidNumberLifecycleTransition);
        }

        SequenceValue = sequenceValue;
        FormattedNumber = formattedNumber;
        IssuedAtUtc = nowUtc;
        Status = NumberLifecycleStatus.Reserved;

        AddDomainEvent(new NumberReserved(Guid.NewGuid(), nowUtc, Id, NumberSeriesId, sequenceValue, formattedNumber.Value));
        return Result.Success();
    }

    /// <summary>
    /// Confirms this reservation as a finalized number -- the point a temporary
    /// reservation (numbering-framework.md's Reservation section: "Draft Payroll, Draft
    /// Employee") becomes permanent because the underlying business record was actually
    /// saved, not abandoned.
    /// </summary>
    public Result MarkGenerated(DateTimeOffset nowUtc)
    {
        if (Status != NumberLifecycleStatus.Reserved || FormattedNumber is null)
        {
            return Result.Failure(NumberingErrors.InvalidNumberLifecycleTransition);
        }

        Status = NumberLifecycleStatus.Generated;
        AddDomainEvent(new NumberGenerated(Guid.NewGuid(), nowUtc, Id, FormattedNumber.Value));
        return Result.Success();
    }

    /// <summary>
    /// Attaches this number to the business record that actually consumes it. Generic
    /// by design -- this framework serves every business module named in its own
    /// Downstream Consumers list, so <paramref name="assignedToType"/>/
    /// <paramref name="assignedToReferenceId"/> are plain strings rather than a
    /// strongly-typed reference to any one consumer's own entity type, which would
    /// wrongly couple this framework to a single caller.
    /// </summary>
    public Result Assign(string? assignedToType, string? assignedToReferenceId, DateTimeOffset nowUtc)
    {
        if (Status != NumberLifecycleStatus.Generated)
        {
            return Result.Failure(NumberingErrors.InvalidNumberLifecycleTransition);
        }

        if (string.IsNullOrWhiteSpace(assignedToType))
        {
            return Result.Failure(NumberingErrors.AssignedToTypeRequired);
        }

        if (string.IsNullOrWhiteSpace(assignedToReferenceId))
        {
            return Result.Failure(NumberingErrors.AssignedToReferenceIdRequired);
        }

        AssignedToType = assignedToType.Trim();
        AssignedToReferenceId = assignedToReferenceId.Trim();
        Status = NumberLifecycleStatus.Assigned;

        AddDomainEvent(new NumberAssigned(Guid.NewGuid(), nowUtc, Id, AssignedToType, AssignedToReferenceId));
        return Result.Success();
    }

    /// <summary>
    /// Re-checks this already-assigned number against its own series' *current*
    /// prefix/format (<paramref name="currentPrefix"/>/<paramref name="currentFormat"/>,
    /// loaded and passed in by the caller -- cross-aggregate data this aggregate never
    /// loads for itself). A real, meaningfully-failable check, not a synthetic
    /// always-true one: if <see cref="NumberSeries.UpdateFormat"/> changed the series'
    /// own configuration after this number was issued, re-formatting from this number's
    /// own recorded <see cref="SequenceValue"/>/<see cref="IssuedAtUtc"/> against
    /// *today's* rules no longer reproduces <see cref="FormattedNumber"/> -- exactly the
    /// data-integrity drift numbering-framework.md's own Validation section names
    /// ("Format... Prefix") as something this framework checks. On success, transitions
    /// to <see cref="NumberLifecycleStatus.Validated"/> and raises no event, matching
    /// the document's own Domain Events list naming only the failure case.
    /// </summary>
    public Result Validate(NumberPrefix currentPrefix, NumberFormat currentFormat, DateTimeOffset nowUtc)
    {
        Guard.AgainstNull(currentPrefix, nameof(currentPrefix));
        Guard.AgainstNull(currentFormat, nameof(currentFormat));

        if (Status != NumberLifecycleStatus.Assigned || SequenceValue is null || FormattedNumber is null || IssuedAtUtc is null)
        {
            return Result.Failure(NumberingErrors.InvalidNumberLifecycleTransition);
        }

        var expected = currentFormat.Format(currentPrefix, SequenceValue.Value, IssuedAtUtc.Value);

        if (!string.Equals(expected, FormattedNumber.Value, StringComparison.Ordinal))
        {
            AddDomainEvent(new NumberValidationFailed(Guid.NewGuid(), nowUtc, Id));
            return Result.Failure(NumberingErrors.NumberFormatMismatch);
        }

        Status = NumberLifecycleStatus.Validated;
        return Result.Success();
    }

    /// <summary>
    /// Abandons this number before it was ever assigned to a real record --
    /// numbering-framework.md's own Reservation section ("Unused reservations should
    /// automatically expire"). Per the AI Implementation Guidance ("Never reuse a
    /// number after a record is deleted or voided; gaps are acceptable, collisions are
    /// not"), this never returns <see cref="SequenceValue"/> to
    /// <see cref="NumberSeries"/> for reuse -- the sequence slot stays permanently
    /// consumed, a gap, not a hole to fill.
    /// </summary>
    public Result Release(string? reason, DateTimeOffset nowUtc)
    {
        if (Status is not (NumberLifecycleStatus.Requested or NumberLifecycleStatus.Reserved or NumberLifecycleStatus.Generated))
        {
            return Result.Failure(NumberingErrors.InvalidNumberLifecycleTransition);
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result.Failure(NumberingErrors.ReleaseReasonRequired);
        }

        Status = NumberLifecycleStatus.Released;
        AddDomainEvent(new NumberReleased(Guid.NewGuid(), nowUtc, Id, reason.Trim()));
        return Result.Success();
    }

    /// <summary>
    /// Terminal from <see cref="NumberLifecycleStatus.Validated"/>. Takes no
    /// <c>nowUtc</c> parameter, unlike every other transition here -- numbering-framework.md's
    /// own Domain Events list names no archival event and this aggregate records no
    /// archival timestamp of its own, so there is nothing here for a clock to date.
    /// </summary>
    public Result Archive()
    {
        if (Status != NumberLifecycleStatus.Validated)
        {
            return Result.Failure(NumberingErrors.InvalidNumberLifecycleTransition);
        }

        Status = NumberLifecycleStatus.Archived;
        return Result.Success();
    }
}
