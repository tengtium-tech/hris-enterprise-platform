using Hris.SharedKernel;

namespace Hris.Foundation.Numbering.Domain;

/// <summary>
/// numbering-framework.md's own Domain Events section names eight events -- every one
/// implemented here, one method per event across <see cref="NumberSeries"/> and
/// <see cref="IssuedNumber"/>. No "NumberValidated" success event exists in that list
/// -- only <see cref="NumberValidationFailed"/> -- so <see cref="IssuedNumber.Validate"/>
/// raises nothing on success, the identical asymmetry file-storage.md's own event list
/// draws between <c>FileValidated</c> and its own unnamed success case elsewhere.
/// </summary>
public sealed record NumberRequested(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    IssuedNumberId IssuedNumberId,
    NumberSeriesId NumberSeriesId) : IDomainEvent;

public sealed record NumberReserved(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    IssuedNumberId IssuedNumberId,
    NumberSeriesId NumberSeriesId,
    long SequenceValue,
    string FormattedNumber) : IDomainEvent;

public sealed record NumberGenerated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    IssuedNumberId IssuedNumberId,
    string FormattedNumber) : IDomainEvent;

public sealed record NumberAssigned(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    IssuedNumberId IssuedNumberId,
    string AssignedToType,
    string AssignedToReferenceId) : IDomainEvent;

public sealed record NumberReleased(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    IssuedNumberId IssuedNumberId,
    string Reason) : IDomainEvent;

public sealed record NumberValidationFailed(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    IssuedNumberId IssuedNumberId) : IDomainEvent;

public sealed record SequenceReset(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    NumberSeriesId NumberSeriesId) : IDomainEvent;

public sealed record NumberSeriesUpdated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    NumberSeriesId NumberSeriesId) : IDomainEvent;
