using FluentAssertions;
using Hris.Foundation.StatutoryReferenceData.Domain;
using Xunit;

namespace Hris.Foundation.StatutoryReferenceData.Tests.Domain;

public sealed class StatutoryCountryCodeTests
{
    [Theory]
    [InlineData("ph", "PH")]
    [InlineData("US", "US")]
    public void Create_Succeeds_AndNormalizesToUppercase(string input, string expected)
    {
        var result = StatutoryCountryCode.Create(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Fails_WhenValueIsMissing(string? value)
    {
        var result = StatutoryCountryCode.Create(value);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(StatutoryReferenceDataErrors.CountryCodeRequired);
    }

    [Fact]
    public void Create_Fails_WhenValueIsNotARecognizedCountry()
    {
        var result = StatutoryCountryCode.Create("ZZ");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(StatutoryReferenceDataErrors.CountryCodeInvalidFormat);
    }
}
