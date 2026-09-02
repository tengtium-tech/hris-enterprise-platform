using FluentAssertions;
using Hris.Foundation.Extension.Domain;
using Xunit;

namespace Hris.Foundation.Extension.Tests.Domain;

public sealed class ExtensionPointKeyTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Fails_WhenValueIsNullOrWhitespace(string? value)
    {
        var result = ExtensionPointKey.Create(value);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ExtensionErrors.ExtensionPointKeyRequired);
    }

    [Fact]
    public void Create_Fails_WhenValueExceeds200Characters()
    {
        var tooLong = new string('a', 201);

        var result = ExtensionPointKey.Create(tooLong);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ExtensionErrors.ExtensionPointKeyTooLong);
    }

    [Fact]
    public void Create_Succeeds_AtExactly200Characters()
    {
        var result = ExtensionPointKey.Create(new string('a', 200));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_Trims()
    {
        var result = ExtensionPointKey.Create("  employee.before-save  ");

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("employee.before-save");
    }

    [Fact]
    public void Equality_IsByValue()
    {
        var first = ExtensionPointKey.Create("employee.before-save").Value;
        var second = ExtensionPointKey.Create("employee.before-save").Value;

        first.Should().Be(second);
    }

    [Fact]
    public void Equality_Differs_ForDifferentKeys()
    {
        var first = ExtensionPointKey.Create("employee.before-save").Value;
        var second = ExtensionPointKey.Create("payroll.after-finalize").Value;

        first.Should().NotBe(second);
    }
}
