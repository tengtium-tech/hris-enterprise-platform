using FluentAssertions;
using Hris.Foundation.FileStorage.Application.Dtos;
using Xunit;

namespace Hris.Foundation.FileStorage.Tests.Application;

/// <summary>
/// Confirms these DTOs behave as proper records (value equality, a real
/// <c>ToString</c>) -- FluentAssertions' <c>BeEquivalentTo</c>, used throughout the
/// handler tests, never exercises a record's own generated members, the identical gap
/// <c>ExtensionDtoTests</c> already closes for its own sibling framework.
/// </summary>
public sealed class FileStorageDtoTests
{
    [Fact]
    public void StoredFileDto_HasValueEquality_AndAUsefulToString()
    {
        var version = new FileVersionDto(
            Guid.NewGuid(), 1, "employee-documents/file.pdf", "Sha256", new string('a', 64), 2048,
            "application/pdf", "AmazonS3", Guid.NewGuid(), TestData.NowUtc);
        var original = new StoredFileDto(Guid.NewGuid(), "employee-documents", "resume.pdf", "Available", version, 1);
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(StoredFileDto));
    }

    [Fact]
    public void FileVersionDto_HasValueEquality_AndAUsefulToString()
    {
        var original = new FileVersionDto(
            Guid.NewGuid(), 1, "employee-documents/file.pdf", "Sha256", new string('a', 64), 2048,
            "application/pdf", "AmazonS3", Guid.NewGuid(), TestData.NowUtc);
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(FileVersionDto));
    }
}
