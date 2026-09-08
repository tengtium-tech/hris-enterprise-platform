using Hris.SharedKernel;

namespace Hris.Modules.Employee.Domain;

/// <summary>
/// A structured postal address. Source: docs/04-modules/employee/domain/value-
/// objects.md's Address ("Components: Address Line 1, Address Line 2, City,
/// Province / State, Postal Code, Country... Country required"). Reused for both
/// Home and Mailing address on <see cref="Employee"/> (each its own nullable
/// <c>OwnsOne</c> property) and for <see cref="EmergencyContact"/>'s own optional
/// address, per employee-contact-information.md's "structured, not free text, to
/// support statutory reporting". Assigned via object-initializer, never through a
/// constructor parameter on the owning entity -- see <see cref="PersonName"/>'s
/// own remarks.
/// </summary>
public sealed class Address : ValueObject
{
    private const int _maxLineLength = 200;
    private const int _maxShortFieldLength = 100;

    public string AddressLine1 { get; }

    public string? AddressLine2 { get; }

    public string City { get; }

    public string? Province { get; }

    public string? PostalCode { get; }

    public string Country { get; }

    private Address(string addressLine1, string? addressLine2, string city, string? province, string? postalCode, string country)
    {
        AddressLine1 = addressLine1;
        AddressLine2 = addressLine2;
        City = city;
        Province = province;
        PostalCode = postalCode;
        Country = country;
    }

    public static Result<Address> Create(
        string? addressLine1, string? addressLine2, string? city, string? province, string? postalCode, string? country)
    {
        if (string.IsNullOrWhiteSpace(addressLine1))
        {
            return Result.Failure<Address>(EmployeeErrors.AddressLine1Required);
        }

        if (string.IsNullOrWhiteSpace(city))
        {
            return Result.Failure<Address>(EmployeeErrors.AddressCityRequired);
        }

        if (string.IsNullOrWhiteSpace(country))
        {
            return Result.Failure<Address>(EmployeeErrors.AddressCountryRequired);
        }

        if (addressLine1.Trim().Length > _maxLineLength || (addressLine2?.Trim().Length ?? 0) > _maxLineLength
            || city.Trim().Length > _maxShortFieldLength || (province?.Trim().Length ?? 0) > _maxShortFieldLength
            || (postalCode?.Trim().Length ?? 0) > 20 || country.Trim().Length > _maxShortFieldLength)
        {
            return Result.Failure<Address>(EmployeeErrors.AddressFieldTooLong);
        }

        return Result.Success(new Address(
            addressLine1.Trim(), Normalize(addressLine2), city.Trim(), Normalize(province), Normalize(postalCode), country.Trim()));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return AddressLine1;
        yield return AddressLine2;
        yield return City;
        yield return Province;
        yield return PostalCode;
        yield return Country;
    }

    public override string ToString() =>
        string.Join(", ", new[] { AddressLine1, AddressLine2, City, Province, PostalCode, Country }.Where(part => !string.IsNullOrWhiteSpace(part)));

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
