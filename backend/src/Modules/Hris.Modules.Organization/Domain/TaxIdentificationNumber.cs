using Hris.SharedKernel;

namespace Hris.Modules.Organization.Domain;

/// <summary>
/// A <see cref="LegalEntity"/>'s statutory tax identifier (examples: TIN, VAT
/// Number, Business Tax Number). Source:
/// docs/04-modules/organization/domain/value-objects.md, TaxIdentificationNumber
/// ("Validation is country-specific."). Kept as a validated, opaque string for the
/// identical reason <see cref="BusinessRegistrationNumber"/> is: country-specific
/// format rules belong to a future specialized module, not this one. Uniqueness
/// (LEG-002, "where required by jurisdiction") is checked by the Application layer
/// against the repository, not by this type.
/// </summary>
public sealed class TaxIdentificationNumber : ValueObject
{
    private const int _maxLength = 50;

    public string Value { get; }

    private TaxIdentificationNumber(string value)
    {
        Value = value;
    }

    public static Result<TaxIdentificationNumber> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<TaxIdentificationNumber>(OrganizationErrors.TaxIdentificationNumberRequired);
        }

        var normalized = value.Trim();

        return normalized.Length > _maxLength
            ? Result.Failure<TaxIdentificationNumber>(OrganizationErrors.TaxIdentificationNumberRequired)
            : Result.Success(new TaxIdentificationNumber(normalized));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
