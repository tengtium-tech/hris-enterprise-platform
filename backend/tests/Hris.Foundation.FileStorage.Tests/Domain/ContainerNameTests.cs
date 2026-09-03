using FluentAssertions;
using Hris.Foundation.FileStorage.Domain;
using Xunit;

namespace Hris.Foundation.FileStorage.Tests.Domain;

public sealed class ContainerNameTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Fails_WhenNullOrWhitespace(string? value)
    {
        var result = ContainerName.Create(value);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FileStorageErrors.ContainerNameRequired);
    }

    [Fact]
    public void Create_Fails_WhenTooLong()
    {
        var result = ContainerName.Create(new string('a', 64));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FileStorageErrors.ContainerNameTooLong);
    }

    [Theory]
    [InlineData("Employee-Documents")]
    [InlineData("employee_documents")]
    [InlineData("employee documents")]
    [InlineData("-employee-documents")]
    [InlineData("employee-documents-")]
    [InlineData("employee--documents")]
    public void Create_Fails_WhenNotLowercaseHyphenatedSlug(string value)
    {
        var result = ContainerName.Create(value);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FileStorageErrors.ContainerNameInvalid);
    }

    [Fact]
    public void Create_Succeeds_WithValidSlug()
    {
        var result = ContainerName.Create("employee-documents");

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("employee-documents");
    }

    [Fact]
    public void Create_TrimsWhitespace()
    {
        var result = ContainerName.Create("  employee-documents  ");

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("employee-documents");
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        var containerName = ContainerName.Create("employee-documents").Value;

        containerName.ToString().Should().Be("employee-documents");
    }

    [Fact]
    public void Equality_IsByValue()
    {
        var first = ContainerName.Create("payroll-files").Value;
        var second = ContainerName.Create("payroll-files").Value;

        first.Should().Be(second);
        (first == second).Should().BeTrue();
    }
}
