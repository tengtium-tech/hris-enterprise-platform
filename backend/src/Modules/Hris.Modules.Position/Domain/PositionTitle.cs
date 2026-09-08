using System.Text.RegularExpressions;
using Hris.SharedKernel;

namespace Hris.Modules.Position.Domain;

/// <summary>
/// The official business title of a <see cref="Position"/> (examples: "Senior
/// Software Engineer", "HR Manager"). Source:
/// docs/04-modules/position/domain/value-objects.md, Position Title ("Required
/// value. Maximum length. Character rules."). No uniqueness rule exists for this
/// field in business-rules.md (validations.md's own "Duplicate Position Title within
/// organizational scope" appears only in an "Examples include" list, not as a firm
/// rule), so unlike <see cref="PositionNumber"/> this type is freely reusable across
/// Positions.
/// </summary>
public sealed partial class PositionTitle : ValueObject
{
    private const int _maxLength = 200;

    public string Value { get; }

    private PositionTitle(string value)
    {
        Value = value;
    }

    public static Result<PositionTitle> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<PositionTitle>(PositionErrors.PositionTitleRequired);
        }

        var normalized = WhitespacePattern().Replace(value.Trim(), " ");

        return normalized.Length > _maxLength
            ? Result.Failure<PositionTitle>(PositionErrors.PositionTitleTooLong)
            : Result.Success(new PositionTitle(normalized));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespacePattern();
}
