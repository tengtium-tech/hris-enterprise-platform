using Hris.SharedKernel;

namespace Hris.Modules.Organization.Domain;

/// <summary>
/// A <see cref="LegalEntity"/>'s legal registration identifier (examples: Company
/// Registration Number, SEC Registration, Government Registration). Source:
/// docs/04-modules/organization/domain/value-objects.md, BusinessRegistrationNumber
/// ("Validation depends on jurisdiction."). Kept as a validated, opaque string
/// rather than a jurisdiction-specific structured format, since this module's own
/// AI Implementation Guidance (domain/legal-entities.md) states country-specific
/// processing belongs to specialized modules, not the Organization Module itself.
/// Uniqueness (LEG-001, platform-wide) is checked by the Application layer against
/// the repository, not by this type.
/// </summary>
public sealed class BusinessRegistrationNumber : ValueObject
{
    private const int _maxLength = 50;

    public string Value { get; }

    private BusinessRegistrationNumber(string value)
    {
        Value = value;
    }

    public static Result<BusinessRegistrationNumber> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<BusinessRegistrationNumber>(OrganizationErrors.BusinessRegistrationNumberRequired);
        }

        var normalized = value.Trim();

        return normalized.Length > _maxLength
            ? Result.Failure<BusinessRegistrationNumber>(OrganizationErrors.BusinessRegistrationNumberRequired)
            : Result.Success(new BusinessRegistrationNumber(normalized));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
