using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Hris.SharedKernel;

namespace Hris.Foundation.Numbering.Domain;

/// <summary>
/// Source: docs/03-foundation/numbering-framework.md, Prefix ("Prefixes identify the
/// business context," examples "EMP", "PAY", "LV", "REC", "TRN", "ORG", "PRF", "DOC").
/// Validated against exactly that shape -- 1-10 uppercase letters or digits, no
/// separator characters of its own, since <see cref="NumberFormat"/> owns where the
/// configured separator is inserted.
/// </summary>
public sealed partial class NumberPrefix : ValueObject
{
    private const int _maxLength = 10;

    public string Value { get; }

    private NumberPrefix(string value)
    {
        Value = value;
    }

    [SuppressMessage(
        "Globalization",
        "CA1308:Normalize strings to uppercase",
        Justification = "Not a lowercase transform -- ToUpperInvariant normalizes to " +
            "the document's own stated uppercase convention (\"EMP\", \"PAY\"), the " +
            "direction CA1308 does not warn about; the rule's own name names the " +
            "opposite transform.")]
    public static Result<NumberPrefix> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<NumberPrefix>(NumberingErrors.PrefixRequired);
        }

        var normalized = value.Trim().ToUpperInvariant();

        if (normalized.Length > _maxLength || !PrefixPattern().IsMatch(normalized))
        {
            return Result.Failure<NumberPrefix>(NumberingErrors.PrefixInvalid);
        }

        return Result.Success(new NumberPrefix(normalized));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    [GeneratedRegex("^[A-Z0-9]{1,10}$")]
    private static partial Regex PrefixPattern();
}
