using Hris.SharedKernel;

namespace Hris.Modules.Organization.Domain;

/// <summary>
/// The unique short code identifying an <see cref="Organization"/> (examples:
/// "CORP", "GLOBAL", "HQ"). Source:
/// docs/04-modules/organization/domain/value-objects.md, OrganizationCode
/// ("Validate format. Normalize casing. Preserve uniqueness constraints.").
/// Normalized to upper invariant, matching every example in that document.
/// Uniqueness (ORG-002, tenant-wide) is checked by the Application layer against the
/// repository, not by this type.
/// </summary>
public sealed class OrganizationCode : ValueObject
{
    private const int _maxLength = 20;

    public string Value { get; }

    private OrganizationCode(string value)
    {
        Value = value;
    }

    public static Result<OrganizationCode> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<OrganizationCode>(OrganizationErrors.OrganizationCodeRequired);
        }

        var normalized = value.Trim().ToUpperInvariant();

        if (normalized.Length > _maxLength)
        {
            return Result.Failure<OrganizationCode>(OrganizationErrors.OrganizationCodeTooLong);
        }

        return Result.Success(new OrganizationCode(normalized));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
