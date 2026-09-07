using System.Text.RegularExpressions;
using Hris.SharedKernel;

namespace Hris.Modules.Organization.Domain;

/// <summary>
/// The name of a <see cref="BusinessUnit"/> (examples: "Corporate", "Operations",
/// "Sales"). Source: docs/04-modules/organization/domain/value-objects.md,
/// BusinessUnitName. Uniqueness (BU-001, within one Organization) is enforced by
/// <see cref="Organization.AddBusinessUnit"/> itself, since every sibling
/// BusinessUnit is already loaded within the same Aggregate instance.
/// </summary>
public sealed partial class BusinessUnitName : ValueObject
{
    private const int _maxLength = 200;

    public string Value { get; }

    private BusinessUnitName(string value)
    {
        Value = value;
    }

    public static Result<BusinessUnitName> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<BusinessUnitName>(OrganizationErrors.BusinessUnitNameRequired);
        }

        var normalized = WhitespacePattern().Replace(value.Trim(), " ");

        return normalized.Length > _maxLength
            ? Result.Failure<BusinessUnitName>(OrganizationErrors.BusinessUnitNameRequired)
            : Result.Success(new BusinessUnitName(normalized));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespacePattern();
}
