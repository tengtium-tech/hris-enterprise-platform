using FluentAssertions;
using Hris.Modules.Organization.Domain;
using Xunit;

namespace Hris.Modules.Organization.Tests.Domain;

public sealed class ValueObjectTests
{
    [Fact]
    public void OrganizationName_NormalizesInternalWhitespace()
    {
        var result = OrganizationName.Create("ABC   Corporation");

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("ABC Corporation");
    }

    [Fact]
    public void OrganizationName_Fails_WhenOnlyWhitespace()
    {
        var result = OrganizationName.Create("   ");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrganizationErrors.OrganizationNameRequired);
    }

    [Fact]
    public void OrganizationCode_NormalizesToUpperInvariant()
    {
        var result = OrganizationCode.Create("corp");

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("CORP");
    }

    [Fact]
    public void OrganizationCode_TwoEqualValues_AreEqual()
    {
        var first = OrganizationCode.Create("CORP").Value;
        var second = OrganizationCode.Create("corp").Value;

        first.Should().Be(second);
    }

    [Fact]
    public void GeographicLocation_Fails_WhenLatitudeIsOutOfRange()
    {
        var result = GeographicLocation.Create(91, 0);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrganizationErrors.GeographicCoordinateOutOfRange);
    }

    [Fact]
    public void GeographicLocation_Fails_WhenLongitudeIsOutOfRange()
    {
        var result = GeographicLocation.Create(0, 181);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrganizationErrors.GeographicCoordinateOutOfRange);
    }

    [Fact]
    public void GeographicLocation_Succeeds_AtTheBoundaryValues()
    {
        var result = GeographicLocation.Create(90, -180);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Address_Fails_WhenLine1IsMissing()
    {
        var result = Address.Create(null, null, "Makati", null, null, "Philippines");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrganizationErrors.AddressLine1Required);
    }

    [Fact]
    public void Address_Succeeds_WithOnlyTheRequiredFields()
    {
        var result = Address.Create("123 Ayala Ave", null, "Makati", null, null, "Philippines");

        result.IsSuccess.Should().BeTrue();
        result.Value.ProvinceOrState.Should().BeNull();
    }
}
