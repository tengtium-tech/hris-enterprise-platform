using System.Globalization;
using Hris.SharedKernel;

namespace Hris.Foundation.Numbering.Domain;

/// <summary>
/// Source: docs/03-foundation/numbering-framework.md, Number Format ("Formats may
/// combine: Prefix, Year, Month, Company Code, Branch Code, Running Number, Suffix...
/// Formats should be configurable"). Covers Prefix + optional Year + optional Month +
/// a zero-padded Running Number, joined by a configurable separator -- exactly the
/// document's own "EMP-2026-000123" and "PAY-2026-08-000021" examples. Company Code,
/// Branch Code, and Suffix are deliberately not modeled as part of a series' own stored
/// configuration: which company or branch a specific request belongs to is a
/// per-request fact a caller supplies, not a fixed property of the series itself, and
/// this Sprint's own build has no organizational-scope integration point yet (the same
/// deferral <see cref="SequenceResetPolicy"/>'s own remarks state for Organization/
/// Department-Based sequences).
///
/// <see cref="Format"/> is this type's single source of truth for what a formatted
/// number looks like -- called identically at generation time
/// (<see cref="IssuedNumber.Reserve"/>'s own caller) and at re-validation time
/// (<see cref="IssuedNumber.Validate"/>), so the two can never silently drift apart.
/// </summary>
public sealed class NumberFormat : ValueObject
{
    private const int _minRunningNumberLength = 1;
    private const int _maxRunningNumberLength = 10;
    private const int _maxSeparatorLength = 3;

    public int RunningNumberLength { get; }

    public bool IncludeYear { get; }

    public bool IncludeMonth { get; }

    public string Separator { get; }

    private NumberFormat(int runningNumberLength, bool includeYear, bool includeMonth, string separator)
    {
        RunningNumberLength = runningNumberLength;
        IncludeYear = includeYear;
        IncludeMonth = includeMonth;
        Separator = separator;
    }

    public static Result<NumberFormat> Create(int runningNumberLength, bool includeYear, bool includeMonth, string? separator)
    {
        if (runningNumberLength is < _minRunningNumberLength or > _maxRunningNumberLength)
        {
            return Result.Failure<NumberFormat>(NumberingErrors.RunningNumberLengthOutOfRange);
        }

        if (string.IsNullOrEmpty(separator))
        {
            return Result.Failure<NumberFormat>(NumberingErrors.SeparatorRequired);
        }

        if (separator.Length > _maxSeparatorLength)
        {
            return Result.Failure<NumberFormat>(NumberingErrors.SeparatorTooLong);
        }

        return Result.Success(new NumberFormat(runningNumberLength, includeYear, includeMonth, separator));
    }

    /// <summary>
    /// Assembles the final identifier string: <c>Prefix[Separator]Year[Separator]Month[Separator]RunningNumber</c>,
    /// including only the components this format enables, zero-padding
    /// <paramref name="sequenceValue"/> to <see cref="RunningNumberLength"/> digits.
    /// <paramref name="referenceDate"/> supplies the Year/Month components -- always
    /// the date the number was actually reserved, per <see cref="IssuedNumber.Reserve"/>,
    /// never the current date at some later read, so a number's own year does not
    /// silently change if it is displayed the following January.
    /// </summary>
    public string Format(NumberPrefix prefix, long sequenceValue, DateTimeOffset referenceDate)
    {
        Guard.AgainstNull(prefix, nameof(prefix));

        var segments = new List<string> { prefix.Value };

        if (IncludeYear)
        {
            segments.Add(referenceDate.Year.ToString(CultureInfo.InvariantCulture));
        }

        if (IncludeMonth)
        {
            segments.Add(referenceDate.Month.ToString("D2", CultureInfo.InvariantCulture));
        }

        segments.Add(sequenceValue.ToString(CultureInfo.InvariantCulture).PadLeft(RunningNumberLength, '0'));

        return string.Join(Separator, segments);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return RunningNumberLength;
        yield return IncludeYear;
        yield return IncludeMonth;
        yield return Separator;
    }
}
