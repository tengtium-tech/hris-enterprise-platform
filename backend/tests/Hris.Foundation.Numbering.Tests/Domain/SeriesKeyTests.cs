using FluentAssertions;
using Hris.Foundation.Numbering.Domain;
using Xunit;

namespace Hris.Foundation.Numbering.Tests.Domain;

public sealed class SeriesKeyTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Fails_WhenNullOrWhitespace(string? value)
    {
        var result = SeriesKey.Create(value);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NumberingErrors.SeriesKeyRequired);
    }

    [Fact]
    public void Create_Fails_WhenTooLong()
    {
        var result = SeriesKey.Create(new string('a', 201));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NumberingErrors.SeriesKeyTooLong);
    }

    [Fact]
    public void Create_TrimsWhitespace()
    {
        var result = SeriesKey.Create("  employee-numbers  ");

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("employee-numbers");
    }

    [Fact]
    public void Equality_IsByValue()
    {
        var first = SeriesKey.Create("employee-numbers").Value;
        var second = SeriesKey.Create("employee-numbers").Value;

        first.Should().Be(second);
        (first == second).Should().BeTrue();
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        var key = SeriesKey.Create("employee-numbers").Value;

        key.ToString().Should().Be("employee-numbers");
    }
}
