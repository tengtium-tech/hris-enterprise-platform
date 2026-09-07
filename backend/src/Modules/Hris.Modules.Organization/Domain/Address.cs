using Hris.SharedKernel;

namespace Hris.Modules.Organization.Domain;

/// <summary>
/// A physical address, owned by <see cref="WorkLocation"/> and <see cref="LegalEntity"/>.
/// Source: docs/04-modules/organization/domain/value-objects.md, Address ("Typical
/// properties include: Address Line 1, Address Line 2, City, Province or State,
/// Postal Code, Country. Address validation should occur during construction.").
/// Only Address Line 1, City, and Country are required; the document does not state
/// Address Line 2, Province/State, or Postal Code as mandatory, and a work location
/// address should not be rejected for lacking a province in a country that does not
/// use one.
/// </summary>
public sealed class Address : ValueObject
{
    public string Line1 { get; }

    public string? Line2 { get; }

    public string City { get; }

    public string? ProvinceOrState { get; }

    public string? PostalCode { get; }

    public string Country { get; }

    private Address(string line1, string? line2, string city, string? provinceOrState, string? postalCode, string country)
    {
        Line1 = line1;
        Line2 = line2;
        City = city;
        ProvinceOrState = provinceOrState;
        PostalCode = postalCode;
        Country = country;
    }

    public static Result<Address> Create(
        string? line1, string? line2, string? city, string? provinceOrState, string? postalCode, string? country)
    {
        if (string.IsNullOrWhiteSpace(line1))
        {
            return Result.Failure<Address>(OrganizationErrors.AddressLine1Required);
        }

        if (string.IsNullOrWhiteSpace(city))
        {
            return Result.Failure<Address>(OrganizationErrors.AddressCityRequired);
        }

        if (string.IsNullOrWhiteSpace(country))
        {
            return Result.Failure<Address>(OrganizationErrors.AddressCountryRequired);
        }

        return Result.Success(new Address(
            line1.Trim(),
            string.IsNullOrWhiteSpace(line2) ? null : line2.Trim(),
            city.Trim(),
            string.IsNullOrWhiteSpace(provinceOrState) ? null : provinceOrState.Trim(),
            string.IsNullOrWhiteSpace(postalCode) ? null : postalCode.Trim(),
            country.Trim()));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Line1;
        yield return Line2;
        yield return City;
        yield return ProvinceOrState;
        yield return PostalCode;
        yield return Country;
    }
}
