using System.Text.RegularExpressions;
using Hris.SharedKernel;

namespace Hris.Foundation.RulesEngine.Domain;

/// <summary>
/// The stable name identifying a business policy, per rules-engine.md's Rule
/// Definition examples ("Overtime Eligibility," "Leave Eligibility," "Payroll Tax
/// Rule"). Structurally identical to <c>ConfigurationKey</c> -- both are
/// dot-segmented, validated business identifiers -- kept as a separate type rather
/// than reused because Rules Engine has no dependency on Configuration Framework's
/// own assembly for this concept alone.
/// </summary>
public sealed partial class RuleKey : ValueObject
{
    private const int _maxLength = 200;

    public string Value { get; }

    private RuleKey(string value)
    {
        Value = value;
    }

    public static Result<RuleKey> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<RuleKey>(RuleErrors.KeyRequired);
        }

        var trimmed = value.Trim();

        if (trimmed.Length > _maxLength)
        {
            return Result.Failure<RuleKey>(RuleErrors.KeyTooLong);
        }

        if (!SegmentPattern().IsMatch(trimmed))
        {
            return Result.Failure<RuleKey>(RuleErrors.KeyInvalidFormat);
        }

        return Result.Success(new RuleKey(trimmed));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"^[A-Za-z][A-Za-z0-9]*(\.[A-Za-z][A-Za-z0-9]*)*$")]
    private static partial Regex SegmentPattern();
}
