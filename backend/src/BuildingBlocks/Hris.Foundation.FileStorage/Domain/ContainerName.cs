using System.Text.RegularExpressions;
using Hris.SharedKernel;

namespace Hris.Foundation.FileStorage.Domain;

/// <summary>
/// The logical container (Bucket/Container) a <see cref="StoredFile"/> belongs to.
/// Source: docs/03-foundation/file-storage.md, Bucket/Container ("Files should be
/// organized into logical containers", examples "employee-documents", "payroll-files").
/// Validated against that same lowercase-hyphenated shape rather than an arbitrary
/// free-text string, since a container name flows into a physical storage path
/// (<see cref="StorageObjectKey"/>) and the AI Implementation Guidance requires never
/// deriving a storage path from unvalidated input.
/// </summary>
public sealed partial class ContainerName : ValueObject
{
    private const int _maxLength = 63;

    public string Value { get; }

    private ContainerName(string value)
    {
        Value = value;
    }

    public static Result<ContainerName> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<ContainerName>(FileStorageErrors.ContainerNameRequired);
        }

        var trimmed = value.Trim();

        if (trimmed.Length > _maxLength)
        {
            return Result.Failure<ContainerName>(FileStorageErrors.ContainerNameTooLong);
        }

        if (!ContainerNamePattern().IsMatch(trimmed))
        {
            return Result.Failure<ContainerName>(FileStorageErrors.ContainerNameInvalid);
        }

        return Result.Success(new ContainerName(trimmed));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex ContainerNamePattern();
}
