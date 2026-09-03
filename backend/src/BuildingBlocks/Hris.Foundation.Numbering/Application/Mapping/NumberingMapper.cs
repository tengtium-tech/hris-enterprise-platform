using Hris.Foundation.Numbering.Application.Dtos;
using Hris.Foundation.Numbering.Domain;

namespace Hris.Foundation.Numbering.Application.Mapping;

/// <summary>
/// Domain-to-DTO mapping, kept as a plain static class rather than a library such as
/// AutoMapper, per mapping.md's own stated preference for explicit mapping code -- the
/// identical choice every other Sprint 3/4 framework's own mapper already establishes.
/// </summary>
internal static class NumberingMapper
{
    public static NumberSeriesDto ToDto(NumberSeries numberSeries) => new(
        numberSeries.Id.Value,
        numberSeries.Key.Value,
        numberSeries.Prefix.Value,
        numberSeries.Format.RunningNumberLength,
        numberSeries.Format.IncludeYear,
        numberSeries.Format.IncludeMonth,
        numberSeries.Format.Separator,
        numberSeries.ResetPolicy.ToString(),
        numberSeries.CurrentSequenceValue,
        numberSeries.LastResetAtUtc);

    public static IssuedNumberDto ToDto(IssuedNumber issuedNumber) => new(
        issuedNumber.Id.Value,
        issuedNumber.NumberSeriesId.Value,
        issuedNumber.SequenceValue,
        issuedNumber.FormattedNumber?.Value,
        issuedNumber.Status.ToString(),
        issuedNumber.AssignedToType,
        issuedNumber.AssignedToReferenceId,
        issuedNumber.RequestedAtUtc,
        issuedNumber.IssuedAtUtc);
}
