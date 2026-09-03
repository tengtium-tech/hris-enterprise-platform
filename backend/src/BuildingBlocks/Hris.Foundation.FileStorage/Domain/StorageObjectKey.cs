using Hris.SharedKernel;

namespace Hris.Foundation.FileStorage.Domain;

/// <summary>
/// The physical location of a <see cref="FileVersion"/>'s content within its
/// <see cref="StorageProviderType"/>. Source: docs/03-foundation/file-storage.md,
/// Storage Object ("Object Identifier... Storage Location"). Validated for shape and,
/// concretely, against a parent-directory traversal segment (<c>..</c>) -- the AI
/// Implementation Guidance's "never derive a storage path from unvalidated user input"
/// stated as a checkable invariant rather than left as unenforced prose: a key
/// containing <c>..</c> is exactly the shape that lets a caller escape its own tenant's
/// or container's own partition of the physical store.
/// </summary>
public sealed class StorageObjectKey : ValueObject
{
    private const int _maxLength = 1024;

    public string Value { get; }

    private StorageObjectKey(string value)
    {
        Value = value;
    }

    public static Result<StorageObjectKey> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<StorageObjectKey>(FileStorageErrors.StorageObjectKeyRequired);
        }

        var trimmed = value.Trim();

        if (trimmed.Length > _maxLength)
        {
            return Result.Failure<StorageObjectKey>(FileStorageErrors.StorageObjectKeyTooLong);
        }

        if (trimmed.Split('/').Any(segment => segment == ".."))
        {
            return Result.Failure<StorageObjectKey>(FileStorageErrors.StorageObjectKeyContainsTraversal);
        }

        return Result.Success(new StorageObjectKey(trimmed));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
