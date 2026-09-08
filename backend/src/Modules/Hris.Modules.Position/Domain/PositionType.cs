using System.Text.RegularExpressions;
using Hris.SharedKernel;

namespace Hris.Modules.Position.Domain;

/// <summary>
/// The tenant-defined workforce category of a <see cref="Position"/> (examples:
/// "Individual Contributor", "People Manager", "Executive"). Source:
/// docs/04-modules/position/domain/business-rules.md, Position Type ("Every Position
/// has a defined Position Type... Position Type must belong to an approved
/// classification"). Unlike Job Family/Classification/Grade, no separate catalog
/// Aggregate is defined for Position Type anywhere in aggregates.md's own four-item
/// Aggregate Overview, and value-objects.md's own Common Value Objects list does not
/// name it as one of its worked examples either -- modeled here as a tenant-free-text
/// Value Object rather than a fifth Aggregate this module's own specification never
/// describes, matching CLAUDE.md's "do not manufacture decision points" guidance
/// where a document is silent on a concrete shape.
/// </summary>
public sealed partial class PositionType : ValueObject
{
    private const int _maxLength = 100;

    public string Value { get; }

    private PositionType(string value)
    {
        Value = value;
    }

    public static Result<PositionType> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<PositionType>(PositionErrors.PositionTypeRequired);
        }

        var normalized = WhitespacePattern().Replace(value.Trim(), " ");

        return normalized.Length > _maxLength
            ? Result.Failure<PositionType>(PositionErrors.PositionTypeTooLong)
            : Result.Success(new PositionType(normalized));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespacePattern();
}
