using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Hris.SharedKernel;

namespace Hris.Foundation.FileStorage.Domain;

/// <summary>
/// Source: docs/03-foundation/file-storage.md, File Integrity ("Checksum Validation")
/// and File Metadata ("Checksum"). Carries its own <see cref="ChecksumAlgorithm"/>
/// rather than assuming one platform-wide constant, so a future second algorithm never
/// requires widening this type's own shape -- only its validated-length table.
/// </summary>
public sealed class Checksum : ValueObject
{
    public ChecksumAlgorithm Algorithm { get; }

    public string Value { get; }

    private Checksum(ChecksumAlgorithm algorithm, string value)
    {
        Algorithm = algorithm;
        Value = value;
    }

    [SuppressMessage(
        "Globalization",
        "CA1308:Normalize strings to uppercase",
        Justification = "A hex checksum is conventionally rendered lowercase, and CA1308's " +
            "usual security concern (a lowercase transform used for a security-sensitive " +
            "comparison prone to culture-specific casing bugs) does not apply here: this " +
            "value is compared only against another already-normalized Checksum's own Value, " +
            "both produced by this same method, and InvariantCulture eliminates the " +
            "locale-dependent casing CA1308 warns about in the first place.")]
    public static Result<Checksum> Create(ChecksumAlgorithm algorithm, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<Checksum>(FileStorageErrors.ChecksumValueRequired);
        }

        var trimmed = value.Trim();
        var expectedLength = ExpectedHexLength(algorithm);

        if (trimmed.Length != expectedLength)
        {
            return Result.Failure<Checksum>(FileStorageErrors.ChecksumValueInvalidLength);
        }

        if (!trimmed.All(Uri.IsHexDigit))
        {
            return Result.Failure<Checksum>(FileStorageErrors.ChecksumValueNotHexadecimal);
        }

        return Result.Success(new Checksum(algorithm, trimmed.ToLowerInvariant()));
    }

    private static int ExpectedHexLength(ChecksumAlgorithm algorithm) => algorithm switch
    {
        ChecksumAlgorithm.Sha256 => 64,
        _ => throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, "Unrecognized checksum algorithm."),
    };

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Algorithm;
        yield return Value;
    }

    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"{Algorithm}:{Value}");
}
