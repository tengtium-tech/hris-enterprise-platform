using Hris.SharedKernel;

namespace Hris.Foundation.Extension.Domain;

/// <summary>
/// An Extension Point's own stable, globally unique identifier -- what a module
/// registers under and what a Hook subscribes against, distinct from its
/// human-readable <see cref="ExtensionPoint.Name"/> (extension-framework.md's own
/// examples use a readable name, "Before Employee Save", not a machine key; this type
/// exists because a Hook needs something stable to reference that a later rename of
/// the display name cannot break). Validated for shape only (required, reasonable
/// length) -- the document gives no explicit key format to enforce, and inventing a
/// strict delimiter pattern it never states would be exactly the kind of invented
/// decision this project's own discipline avoids.
/// </summary>
public sealed class ExtensionPointKey : ValueObject
{
    private const int _maxLength = 200;

    public string Value { get; }

    private ExtensionPointKey(string value)
    {
        Value = value;
    }

    public static Result<ExtensionPointKey> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<ExtensionPointKey>(ExtensionErrors.ExtensionPointKeyRequired);
        }

        var trimmed = value.Trim();

        if (trimmed.Length > _maxLength)
        {
            return Result.Failure<ExtensionPointKey>(ExtensionErrors.ExtensionPointKeyTooLong);
        }

        return Result.Success(new ExtensionPointKey(trimmed));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
