using FluentAssertions;
using Hris.Foundation.FileStorage.Domain;
using Xunit;

namespace Hris.Foundation.FileStorage.Tests.Domain;

public sealed class MimeTypeTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Fails_WhenNullOrWhitespace(string? value)
    {
        var result = MimeType.Create(value);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FileStorageErrors.MimeTypeRequired);
    }

    [Theory]
    [InlineData("application")]
    [InlineData("/pdf")]
    [InlineData("application/")]
    [InlineData("application//pdf")]
    public void Create_Fails_WhenNotTypeSubtypeShape(string value)
    {
        var result = MimeType.Create(value);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FileStorageErrors.MimeTypeInvalid);
    }

    [Fact]
    public void Create_Fails_WhenTooLong()
    {
        var result = MimeType.Create("application/" + new string('a', 250));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FileStorageErrors.MimeTypeInvalid);
    }

    [Theory]
    [InlineData("application/pdf")]
    [InlineData("image/png")]
    [InlineData("application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    public void Create_Succeeds_WithValidShape(string value)
    {
        var result = MimeType.Create(value);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(value);
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        var mimeType = MimeType.Create("application/pdf").Value;

        mimeType.ToString().Should().Be("application/pdf");
    }

    [Fact]
    public void Equality_IsByValue()
    {
        var first = MimeType.Create("application/pdf").Value;
        var second = MimeType.Create("application/pdf").Value;

        first.Should().Be(second);
        (first == second).Should().BeTrue();
    }
}
