using FluentAssertions;
using Hris.Modules.Employee.Domain;
using Xunit;

namespace Hris.Modules.Employee.Tests.Domain;

public sealed class ValueObjectTests
{
    [Fact]
    public void EmployeeNumber_WithValidValue_Succeeds()
    {
        var result = EmployeeNumber.Create("EMP-000001");

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("EMP-000001");
    }

    [Fact]
    public void EmployeeNumber_WhenEmpty_Fails()
    {
        var result = EmployeeNumber.Create(string.Empty);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmployeeErrors.EmployeeNumberRequired);
    }

    [Fact]
    public void EmployeeNumber_WhenTooLong_Fails()
    {
        var result = EmployeeNumber.Create(new string('A', 51));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmployeeErrors.EmployeeNumberTooLong);
    }

    [Fact]
    public void PersonName_WithValidData_Succeeds()
    {
        var result = PersonName.Create("Juan", "Santos", "Dela Cruz", "Mr.", "Jr.", "Johnny");

        result.IsSuccess.Should().BeTrue();
        result.Value.FirstName.Should().Be("Juan");
        result.Value.PreferredName.Should().Be("Johnny");
    }

    [Fact]
    public void PersonName_WithoutFirstName_Fails()
    {
        var result = PersonName.Create(null, null, "Dela Cruz", null, null, null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmployeeErrors.FirstNameRequired);
    }

    [Fact]
    public void PersonName_WithoutLastName_Fails()
    {
        var result = PersonName.Create("Juan", null, null, null, null, null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmployeeErrors.LastNameRequired);
    }

    [Fact]
    public void PersonName_WithFieldTooLong_Fails()
    {
        var result = PersonName.Create(new string('A', 101), null, "Dela Cruz", null, null, null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmployeeErrors.PersonNameFieldTooLong);
    }

    [Fact]
    public void PersonName_ToString_JoinsNonEmptyParts()
    {
        var result = PersonName.Create("Juan", null, "Dela Cruz", "Mr.", null, null);

        result.Value.ToString().Should().Be("Mr. Juan Dela Cruz");
    }

    [Theory]
    [InlineData("+63 917 123 4567")]
    [InlineData("0917-123-4567")]
    public void PhoneNumber_WithValidFormat_Succeeds(string value)
    {
        var result = PhoneNumber.Create(value);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void PhoneNumber_WhenEmpty_Fails()
    {
        var result = PhoneNumber.Create(string.Empty);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmployeeErrors.PhoneNumberInvalidFormat);
    }

    [Fact]
    public void PhoneNumber_WithInvalidCharacters_Fails()
    {
        var result = PhoneNumber.Create("not-a-phone");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmployeeErrors.PhoneNumberInvalidFormat);
    }

    [Fact]
    public void PhoneNumber_WhenTooLong_Fails()
    {
        var result = PhoneNumber.Create("+" + new string('1', 40));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmployeeErrors.PhoneNumberTooLong);
    }

    [Fact]
    public void Address_WithValidData_Succeeds()
    {
        var result = Address.Create("123 Main St", "Unit 4", "Manila", "Metro Manila", "1000", "Philippines");

        result.IsSuccess.Should().BeTrue();
        result.Value.City.Should().Be("Manila");
    }

    [Fact]
    public void Address_WithoutAddressLine1_Fails()
    {
        var result = Address.Create(null, null, "Manila", null, null, "Philippines");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmployeeErrors.AddressLine1Required);
    }

    [Fact]
    public void Address_WithoutCity_Fails()
    {
        var result = Address.Create("123 Main St", null, null, null, null, "Philippines");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmployeeErrors.AddressCityRequired);
    }

    [Fact]
    public void Address_WithoutCountry_Fails()
    {
        var result = Address.Create("123 Main St", null, "Manila", null, null, null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmployeeErrors.AddressCountryRequired);
    }

    [Fact]
    public void Address_WithFieldTooLong_Fails()
    {
        var result = Address.Create(new string('A', 201), null, "Manila", null, null, "Philippines");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmployeeErrors.AddressFieldTooLong);
    }

    [Theory]
    [InlineData("123-456-789")]
    [InlineData("123456789")]
    public void Tin_WithValidFormat_Succeeds(string value)
    {
        var result = Tin.Create(value);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Tin_WithInvalidFormat_Fails()
    {
        var result = Tin.Create("not-a-tin");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmployeeErrors.TinInvalidFormat);
    }

    [Fact]
    public void SssNumber_WithValidFormat_Succeeds()
    {
        var result = SssNumber.Create("12-3456789-0");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void SssNumber_WithInvalidFormat_Fails()
    {
        var result = SssNumber.Create("bad-value");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmployeeErrors.SssNumberInvalidFormat);
    }

    [Fact]
    public void PhilHealthNumber_WithValidFormat_Succeeds()
    {
        var result = PhilHealthNumber.Create("12-345678901-2");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void PhilHealthNumber_WithInvalidFormat_Fails()
    {
        var result = PhilHealthNumber.Create("bad-value");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmployeeErrors.PhilHealthNumberInvalidFormat);
    }

    [Fact]
    public void PagIbigNumber_WithValidFormat_Succeeds()
    {
        var result = PagIbigNumber.Create("1234-5678-9012");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void PagIbigNumber_WithInvalidFormat_Fails()
    {
        var result = PagIbigNumber.Create("bad-value");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmployeeErrors.PagIbigNumberInvalidFormat);
    }

    [Fact]
    public void GsisNumber_WithValidFormat_Succeeds()
    {
        var result = GsisNumber.Create("12345678");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void GsisNumber_WithInvalidFormat_Fails()
    {
        var result = GsisNumber.Create("bad-value");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmployeeErrors.GsisNumberInvalidFormat);
    }

    [Fact]
    public void BankAccount_WithValidData_Succeeds()
    {
        var result = BankAccount.Create("BDO", "Makati", "Juan Dela Cruz", "1234567890", "BNORPHMM");

        result.IsSuccess.Should().BeTrue();
        result.Value.BankName.Should().Be("BDO");
    }

    [Fact]
    public void BankAccount_WithoutBankName_Fails()
    {
        var result = BankAccount.Create(null, null, null, "1234567890", null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmployeeErrors.BankNameRequired);
    }

    [Fact]
    public void BankAccount_WithoutAccountNumber_Fails()
    {
        var result = BankAccount.Create("BDO", null, null, null, null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmployeeErrors.BankAccountNumberRequired);
    }

    [Fact]
    public void BankAccount_WithFieldTooLong_Fails()
    {
        var result = BankAccount.Create(new string('A', 101), null, null, "1234567890", null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmployeeErrors.BankAccountFieldTooLong);
    }
}
