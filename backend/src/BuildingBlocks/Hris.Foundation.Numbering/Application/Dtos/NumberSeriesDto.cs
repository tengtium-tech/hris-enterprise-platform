namespace Hris.Foundation.Numbering.Application.Dtos;

/// <summary>
/// The read-side shape <c>GetNumberSeriesQuery</c> returns, per dto-design.md's own
/// convention.
/// </summary>
public sealed record NumberSeriesDto(
    Guid NumberSeriesId,
    string Key,
    string Prefix,
    int RunningNumberLength,
    bool IncludeYear,
    bool IncludeMonth,
    string Separator,
    string ResetPolicy,
    long CurrentSequenceValue,
    DateTimeOffset? LastResetAtUtc);
