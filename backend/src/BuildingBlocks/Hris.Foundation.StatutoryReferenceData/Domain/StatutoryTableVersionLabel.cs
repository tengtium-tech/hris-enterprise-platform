using System.Text.RegularExpressions;
using Hris.SharedKernel;

namespace Hris.Foundation.StatutoryReferenceData.Domain;

/// <summary>
/// A table version's own label, in the exact <c>YYYY-MM</c> shape
/// statutory-reference-data.md's own Location section names for its fixture files
/// (<c>&lt;program&gt;-&lt;effective-year&gt;-&lt;effective-month&gt;.yaml</c>, e.g.
/// "2025-01" for <c>sss-2025-01.yaml</c>) -- a real, document-stated format, unlike
/// <c>ScheduleExpression</c>'s own deliberately unformatted string, so this Value
/// Object validates shape rather than deferring to the owning aggregate.
/// </summary>
public sealed partial class StatutoryTableVersionLabel : ValueObject
{
    public string Value { get; }

    private StatutoryTableVersionLabel(string value)
    {
        Value = value;
    }

    public static Result<StatutoryTableVersionLabel> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<StatutoryTableVersionLabel>(StatutoryReferenceDataErrors.VersionLabelRequired);
        }

        var trimmed = value.Trim();

        return !VersionLabelPattern().IsMatch(trimmed)
            ? Result.Failure<StatutoryTableVersionLabel>(StatutoryReferenceDataErrors.VersionLabelInvalidFormat)
            : Result.Success(new StatutoryTableVersionLabel(trimmed));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"^\d{4}-(0[1-9]|1[0-2])$")]
    private static partial Regex VersionLabelPattern();
}
