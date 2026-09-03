using FluentAssertions;
using Hris.Foundation.Search.Domain;
using Xunit;

namespace Hris.Foundation.Search.Tests.Domain;

public sealed class SearchEntityTypeTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Fails_WhenNullOrWhitespace(string? value)
    {
        var result = SearchEntityType.Create(value);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SearchErrors.EntityTypeRequired);
    }

    [Theory]
    [InlineData("1EMPLOYEE")]
    [InlineData("EMPLOYEE-RECORD")]
    [InlineData("employee record")]
    public void Create_Fails_WhenInvalidShape(string value)
    {
        var result = SearchEntityType.Create(value);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SearchErrors.EntityTypeInvalid);
    }

    [Fact]
    public void Create_Succeeds_AndNormalizesToUppercase()
    {
        var result = SearchEntityType.Create("employee");

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("EMPLOYEE");
    }

    [Fact]
    public void Equality_IsByValue()
    {
        var first = SearchEntityType.Create("EMPLOYEE").Value;
        var second = SearchEntityType.Create("employee").Value;

        first.Should().Be(second);
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        var entityType = SearchEntityType.Create("employee").Value;

        entityType.ToString().Should().Be("EMPLOYEE");
    }
}
