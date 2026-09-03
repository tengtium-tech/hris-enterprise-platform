using Hris.Foundation.Numbering.Domain;

namespace Hris.Foundation.Numbering.Tests;

/// <summary>
/// Valid-default builders per docs/09-testing/unit-and-integration-testing.md 2.4:
/// "Construct aggregates through builders that supply valid defaults, so each test
/// specifies only the values relevant to what it verifies." A fixed clock
/// (<see cref="NowUtc"/>), never <c>DateTimeOffset.UtcNow</c>, per that same document's
/// own 2.1 ("must not touch... a clock").
/// </summary>
internal static class TestData
{
    public static readonly DateTimeOffset NowUtc = new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

    public static SeriesKey NewSeriesKey(string? value = null) =>
        SeriesKey.Create(value ?? $"employee-numbers-{Guid.NewGuid():N}").Value;

    public static NumberPrefix NewPrefix(string? value = null) => NumberPrefix.Create(value ?? "EMP").Value;

    public static NumberFormat NewFormat(int runningNumberLength = 6, bool includeYear = true, bool includeMonth = false, string separator = "-") =>
        NumberFormat.Create(runningNumberLength, includeYear, includeMonth, separator).Value;

    public static NumberSeries RegisteredSeries(
        SeriesKey? key = null, NumberPrefix? prefix = null, NumberFormat? format = null, SequenceResetPolicy resetPolicy = SequenceResetPolicy.Never) =>
        NumberSeries.Register(key ?? NewSeriesKey(), prefix ?? NewPrefix(), format ?? NewFormat(), resetPolicy).Value;

    /// <summary>A series whose durable sequence has already advanced past zero, simulating what a real atomic increment would have produced.</summary>
    public static NumberSeries SeriesWithSequenceValue(long sequenceValue, NumberPrefix? prefix = null, NumberFormat? format = null)
    {
        var series = RegisteredSeries(prefix: prefix, format: format);
        series.ReconcileSequenceValueAfterAtomicIncrement(sequenceValue);
        return series;
    }

    public static IssuedNumber RequestedNumber(NumberSeriesId? numberSeriesId = null, DateTimeOffset? nowUtc = null) =>
        IssuedNumber.Request(numberSeriesId ?? new NumberSeriesId(Guid.NewGuid()), nowUtc ?? NowUtc).Value;

    public static IssuedNumber ReservedNumber(
        NumberSeriesId? numberSeriesId = null, long sequenceValue = 1, FormattedNumber? formattedNumber = null, DateTimeOffset? nowUtc = null)
    {
        var issuedNumber = RequestedNumber(numberSeriesId, nowUtc);
        issuedNumber.Reserve(sequenceValue, formattedNumber ?? FormattedNumber.Create("EMP-2026-000001").Value, nowUtc ?? NowUtc);
        return issuedNumber;
    }

    public static IssuedNumber GeneratedNumber(
        NumberSeriesId? numberSeriesId = null, long sequenceValue = 1, FormattedNumber? formattedNumber = null, DateTimeOffset? nowUtc = null)
    {
        var issuedNumber = ReservedNumber(numberSeriesId, sequenceValue, formattedNumber, nowUtc);
        issuedNumber.MarkGenerated(nowUtc ?? NowUtc);
        return issuedNumber;
    }

    public static IssuedNumber AssignedNumber(
        NumberSeriesId? numberSeriesId = null,
        long sequenceValue = 1,
        FormattedNumber? formattedNumber = null,
        string assignedToType = "Employee",
        string assignedToReferenceId = "EMP-0001",
        DateTimeOffset? nowUtc = null)
    {
        var issuedNumber = GeneratedNumber(numberSeriesId, sequenceValue, formattedNumber, nowUtc);
        issuedNumber.Assign(assignedToType, assignedToReferenceId, nowUtc ?? NowUtc);
        return issuedNumber;
    }
}
