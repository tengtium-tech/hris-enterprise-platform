using FluentAssertions;
using Hris.Foundation.FileStorage.Domain;
using Xunit;

namespace Hris.Foundation.FileStorage.Tests.Domain;

public sealed class StorageObjectKeyTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Fails_WhenNullOrWhitespace(string? value)
    {
        var result = StorageObjectKey.Create(value);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FileStorageErrors.StorageObjectKeyRequired);
    }

    [Fact]
    public void Create_Fails_WhenTooLong()
    {
        var result = StorageObjectKey.Create(new string('a', 1025));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FileStorageErrors.StorageObjectKeyTooLong);
    }

    [Theory]
    [InlineData("../secrets/config.json")]
    [InlineData("tenant-a/../tenant-b/file.pdf")]
    [InlineData("tenant-a/documents/..")]
    public void Create_Fails_WhenContainingParentDirectoryTraversal(string value)
    {
        var result = StorageObjectKey.Create(value);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FileStorageErrors.StorageObjectKeyContainsTraversal);
    }

    [Fact]
    public void Create_Succeeds_WithValidKey()
    {
        var result = StorageObjectKey.Create("tenant-a/employee-documents/2026/09/file.pdf");

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("tenant-a/employee-documents/2026/09/file.pdf");
    }

    [Fact]
    public void Create_DoesNotFalsePositive_OnDotsWithinASegment()
    {
        var result = StorageObjectKey.Create("reports/2026/annual..summary.pdf");

        result.IsSuccess.Should().BeTrue();
    }
}
