using System.Text.RegularExpressions;
using Hris.SharedKernel;

namespace Hris.Modules.Organization.Domain;

/// <summary>
/// The official name of an <see cref="Organization"/>. Source:
/// docs/04-modules/organization/domain/value-objects.md, OrganizationName
/// ("Validate organization names. Normalize whitespace. Enforce length rules.").
/// Uniqueness (ORG-001, tenant-wide) cannot be enforced by a Value Object in
/// isolation and is checked by the Application layer against the repository before
/// <see cref="Organization.Create"/> is called.
/// </summary>
public sealed partial class OrganizationName : ValueObject
{
    private const int _maxLength = 200;

    public string Value { get; }

    private OrganizationName(string value)
    {
        Value = value;
    }

    public static Result<OrganizationName> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<OrganizationName>(OrganizationErrors.OrganizationNameRequired);
        }

        var normalized = WhitespacePattern().Replace(value.Trim(), " ");

        if (normalized.Length > _maxLength)
        {
            return Result.Failure<OrganizationName>(OrganizationErrors.OrganizationNameTooLong);
        }

        return Result.Success(new OrganizationName(normalized));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespacePattern();
}
