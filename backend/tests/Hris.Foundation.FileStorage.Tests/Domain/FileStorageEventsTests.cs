using FluentAssertions;
using Hris.Foundation.FileStorage.Domain;
using Xunit;

namespace Hris.Foundation.FileStorage.Tests.Domain;

/// <summary>
/// docs/09-testing/unit-and-integration-testing.md 2.2: "Equality is by value, not
/// reference." These nine records are Domain Events, not Value Objects, but the same
/// expectation applies to any immutable data-carrying type this framework hands to a
/// caller -- the identical shape ExtensionEventsTests already establishes.
/// </summary>
public sealed class FileStorageEventsTests
{
    [Fact]
    public void FileUploaded_HasValueEquality_AndAUsefulToString()
    {
        var original = new FileUploaded(
            Guid.NewGuid(), TestData.NowUtc, new StoredFileId(Guid.NewGuid()), new FileVersionId(Guid.NewGuid()),
            1, TestData.NewStorageObjectKey(), StorageProviderType.AmazonS3);
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(FileUploaded));
    }

    [Fact]
    public void FileValidated_HasValueEquality_AndAUsefulToString()
    {
        var original = new FileValidated(Guid.NewGuid(), TestData.NowUtc, new StoredFileId(Guid.NewGuid()), new FileVersionId(Guid.NewGuid()));
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(FileValidated));
    }

    [Fact]
    public void FileStored_HasValueEquality_AndAUsefulToString()
    {
        var original = new FileStored(Guid.NewGuid(), TestData.NowUtc, new StoredFileId(Guid.NewGuid()), new FileVersionId(Guid.NewGuid()), 1);
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(FileStored));
    }

    [Fact]
    public void FileDownloaded_HasValueEquality_AndAUsefulToString()
    {
        var original = new FileDownloaded(
            Guid.NewGuid(), TestData.NowUtc, new StoredFileId(Guid.NewGuid()), new FileVersionId(Guid.NewGuid()), TestData.UploaderUserId);
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(FileDownloaded));
    }

    [Fact]
    public void FileArchived_HasValueEquality_AndAUsefulToString()
    {
        var original = new FileArchived(Guid.NewGuid(), TestData.NowUtc, new StoredFileId(Guid.NewGuid()));
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(FileArchived));
    }

    [Fact]
    public void FileDeleted_HasValueEquality_AndAUsefulToString()
    {
        var original = new FileDeleted(Guid.NewGuid(), TestData.NowUtc, new StoredFileId(Guid.NewGuid()));
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(FileDeleted));
    }

    [Fact]
    public void FileRestored_HasValueEquality_AndAUsefulToString()
    {
        var original = new FileRestored(Guid.NewGuid(), TestData.NowUtc, new StoredFileId(Guid.NewGuid()));
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(FileRestored));
    }

    [Fact]
    public void FileIntegrityVerified_HasValueEquality_AndAUsefulToString()
    {
        var original = new FileIntegrityVerified(
            Guid.NewGuid(), TestData.NowUtc, new StoredFileId(Guid.NewGuid()), new FileVersionId(Guid.NewGuid()), true);
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(FileIntegrityVerified));
    }

    [Fact]
    public void StorageProviderChanged_HasValueEquality_AndAUsefulToString()
    {
        var original = new StorageProviderChanged(
            Guid.NewGuid(), TestData.NowUtc, new StoredFileId(Guid.NewGuid()), new FileVersionId(Guid.NewGuid()),
            StorageProviderType.Local, StorageProviderType.AmazonS3);
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(StorageProviderChanged));
    }
}
