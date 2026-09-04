using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Hris.SharedKernel;

namespace Hris.Foundation.StatutoryReferenceData.Domain;

/// <summary>
/// A statutory program's own stable, unique-per-country key -- statutory-reference-data.md's
/// own Country Scoping tree examples ("SSS", "PhilHealth", "Pag-IBIG", "BIR withholding")
/// and Government Sector Variance's "GSIS is represented as a separate statutory program."
/// Normalized to uppercase, matching <c>NumberPrefix</c>'s own "let the document's own
/// stated vocabulary be the shape, not a closed enum" choice -- statutory-reference-data.md's
/// own Country Scoping tree states plainly that "supporting an additional country means
/// supplying that country's tables against the same structure," which a closed C#
/// <c>enum</c> here would silently foreclose.
/// </summary>
public sealed partial class StatutoryProgramCode : ValueObject
{
    private const int _maxLength = 50;

    public string Value { get; }

    private StatutoryProgramCode(string value)
    {
        Value = value;
    }

    [SuppressMessage(
        "Globalization",
        "CA1308:Normalize strings to uppercase",
        Justification = "Not a lowercase transform -- ToUpperInvariant normalizes to " +
            "this framework's own stated uppercase convention (\"SSS\", \"BIR_WITHHOLDING\"), " +
            "the direction CA1308 does not warn about; the rule's own name names the " +
            "opposite transform.")]
    public static Result<StatutoryProgramCode> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<StatutoryProgramCode>(StatutoryReferenceDataErrors.ProgramCodeRequired);
        }

        var normalized = value.Trim().ToUpperInvariant();

        if (normalized.Length > _maxLength || !ProgramCodePattern().IsMatch(normalized))
        {
            return Result.Failure<StatutoryProgramCode>(StatutoryReferenceDataErrors.ProgramCodeInvalidFormat);
        }

        return Result.Success(new StatutoryProgramCode(normalized));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    [GeneratedRegex("^[A-Z0-9_]{1,50}$")]
    private static partial Regex ProgramCodePattern();
}
