using System.Text.RegularExpressions;
using Hris.SharedKernel;

namespace Hris.Modules.Organization.Domain;

/// <summary>
/// The name of a <see cref="Department"/> (examples: "Human Resources", "Finance",
/// "Information Technology"). Source:
/// docs/04-modules/organization/domain/value-objects.md, DepartmentName. Uniqueness
/// (DEPT-001, within one Division) is enforced by
/// <see cref="Organization.AddDepartment"/> itself.
/// </summary>
public sealed partial class DepartmentName : ValueObject
{
    private const int _maxLength = 200;

    public string Value { get; }

    private DepartmentName(string value)
    {
        Value = value;
    }

    public static Result<DepartmentName> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<DepartmentName>(OrganizationErrors.DepartmentNameRequired);
        }

        var normalized = WhitespacePattern().Replace(value.Trim(), " ");

        return normalized.Length > _maxLength
            ? Result.Failure<DepartmentName>(OrganizationErrors.DepartmentNameRequired)
            : Result.Success(new DepartmentName(normalized));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespacePattern();
}
