using FluentAssertions;
using Hris.Foundation.Numbering.Domain;
using Xunit;

namespace Hris.Foundation.Numbering.Tests.Domain;

public sealed class NumberPrefixTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Fails_WhenNullOrWhitespace(string? value)
    {
        var result = NumberPrefix.Create(value);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NumberingErrors.PrefixRequired);
    }

    [Theory]
    [InlineData("TOOLONGPREFIX")]
    [InlineData("EMP-01")]
    [InlineData("emp 01")]
    public void Create_Fails_WhenInvalidShape(string value)
    {
        var result = NumberPrefix.Create(value);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NumberingErrors.PrefixInvalid);
    }

    [Fact]
    public void Create_Succeeds_AndNormalizesToUppercase()
    {
        var result = NumberPrefix.Create("emp");

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("EMP");
    }

    [Fact]
    public void Equality_IsByValue()
    {
        var first = NumberPrefix.Create("EMP").Value;
        var second = NumberPrefix.Create("emp").Value;

        first.Should().Be(second);
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        var prefix = NumberPrefix.Create("emp").Value;

        prefix.ToString().Should().Be("EMP");
    }
}
