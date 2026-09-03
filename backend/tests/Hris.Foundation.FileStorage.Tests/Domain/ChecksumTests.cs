using FluentAssertions;
using Hris.Foundation.FileStorage.Domain;
using Xunit;

namespace Hris.Foundation.FileStorage.Tests.Domain;

public sealed class ChecksumTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Fails_WhenNullOrWhitespace(string? value)
    {
        var result = Checksum.Create(ChecksumAlgorithm.Sha256, value);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FileStorageErrors.ChecksumValueRequired);
    }

    [Theory]
    [InlineData(63)]
    [InlineData(65)]
    public void Create_Fails_WhenLengthDoesNotMatchAlgorithm(int length)
    {
        var result = Checksum.Create(ChecksumAlgorithm.Sha256, new string('a', length));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FileStorageErrors.ChecksumValueInvalidLength);
    }

    [Fact]
    public void Create_Fails_WhenNotHexadecimal()
    {
        var result = Checksum.Create(ChecksumAlgorithm.Sha256, new string('g', 64));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FileStorageErrors.ChecksumValueNotHexadecimal);
    }

    [Fact]
    public void Create_Succeeds_AndNormalizesToLowercase()
    {
        var result = Checksum.Create(ChecksumAlgorithm.Sha256, new string('A', 64));

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(new string('a', 64));
    }

    [Fact]
    public void Equality_IsByAlgorithmAndValue()
    {
        var first = Checksum.Create(ChecksumAlgorithm.Sha256, new string('a', 64)).Value;
        var second = Checksum.Create(ChecksumAlgorithm.Sha256, new string('a', 64)).Value;

        first.Should().Be(second);
        (first == second).Should().BeTrue();
    }

    [Fact]
    public void Inequality_WhenValueDiffers()
    {
        var first = Checksum.Create(ChecksumAlgorithm.Sha256, new string('a', 64)).Value;
        var second = Checksum.Create(ChecksumAlgorithm.Sha256, new string('b', 64)).Value;

        (first != second).Should().BeTrue();
    }

    [Fact]
    public void Create_Throws_ForAnUnrecognizedAlgorithm()
    {
        var act = () => Checksum.Create((ChecksumAlgorithm)999, new string('a', 64));

        act.Should().Throw<ArgumentOutOfRangeException>("the expected-length switch has no case, and therefore no valid length, for an algorithm this type does not know");
    }

    [Fact]
    public void ToString_RoundTrips_ThroughParse()
    {
        var checksum = Checksum.Create(ChecksumAlgorithm.Sha256, new string('a', 64)).Value;

        var rendered = checksum.ToString();
        var parts = rendered.Split(':', 2);
        var algorithm = Enum.Parse<ChecksumAlgorithm>(parts[0]);
        var roundTripped = Checksum.Create(algorithm, parts[1]).Value;

        roundTripped.Should().Be(checksum);
    }
}
