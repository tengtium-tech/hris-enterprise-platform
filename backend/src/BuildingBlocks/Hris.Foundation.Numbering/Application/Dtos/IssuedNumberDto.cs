namespace Hris.Foundation.Numbering.Application.Dtos;

/// <summary>
/// The read-side shape <c>GetIssuedNumberQuery</c>/<c>ListIssuedNumbersForSeriesQuery</c>
/// return, per dto-design.md's own convention.
/// </summary>
public sealed record IssuedNumberDto(
    Guid IssuedNumberId,
    Guid NumberSeriesId,
    long? SequenceValue,
    string? FormattedNumber,
    string Status,
    string? AssignedToType,
    string? AssignedToReferenceId,
    DateTimeOffset RequestedAtUtc,
    DateTimeOffset? IssuedAtUtc);
