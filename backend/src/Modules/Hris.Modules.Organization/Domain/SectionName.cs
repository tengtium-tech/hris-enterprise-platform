using System.Text.RegularExpressions;
using Hris.SharedKernel;

namespace Hris.Modules.Organization.Domain;

/// <summary>
/// The name of a <see cref="Section"/> (examples: "Recruitment", "Compensation",
/// "Employee Relations"). Source:
/// docs/04-modules/organization/domain/entities.md, Section. Uniqueness (SEC-001's
/// own "Sections belong to a single Department" plus the platform's general
/// per-parent uniqueness convention) is enforced by
/// <see cref="Organization.AddSection"/> itself.
/// </summary>
public sealed partial class SectionName : ValueObject
{
    private const int _maxLength = 200;

    public string Value { get; }

    private SectionName(string value)
    {
        Value = value;
    }

    public static Result<SectionName> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<SectionName>(OrganizationErrors.SectionNameRequired);
        }

        var normalized = WhitespacePattern().Replace(value.Trim(), " ");

        return normalized.Length > _maxLength
            ? Result.Failure<SectionName>(OrganizationErrors.SectionNameRequired)
            : Result.Success(new SectionName(normalized));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespacePattern();
}
