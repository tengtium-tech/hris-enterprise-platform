using System.Text.RegularExpressions;
using Hris.SharedKernel;

namespace Hris.Modules.Organization.Domain;

/// <summary>
/// The name of a <see cref="Division"/> (examples: "Software Division", "Hardware
/// Division", "Consumer Products"). Source:
/// docs/04-modules/organization/domain/entities.md, Division. Not separately named in
/// value-objects.md's own worked examples, but that document's Entity Usage table and
/// business-rules.md's DIV-001 both treat it as a first-class validated name the same
/// as every other level, so it follows the identical shape. Uniqueness (DIV-001,
/// within one BusinessUnit) is enforced by <see cref="Organization.AddDivision"/>
/// itself.
/// </summary>
public sealed partial class DivisionName : ValueObject
{
    private const int _maxLength = 200;

    public string Value { get; }

    private DivisionName(string value)
    {
        Value = value;
    }

    public static Result<DivisionName> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<DivisionName>(OrganizationErrors.DivisionNameRequired);
        }

        var normalized = WhitespacePattern().Replace(value.Trim(), " ");

        return normalized.Length > _maxLength
            ? Result.Failure<DivisionName>(OrganizationErrors.DivisionNameRequired)
            : Result.Success(new DivisionName(normalized));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespacePattern();
}
