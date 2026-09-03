using System.Text.RegularExpressions;
using Hris.SharedKernel;

namespace Hris.Foundation.FileStorage.Domain;

/// <summary>
/// Source: docs/03-foundation/file-storage.md, File Metadata ("MIME Type") and AI
/// Implementation Guidance ("Validate content type and size on upload; never trust
/// client-supplied metadata"). Validated for the standard <c>type/subtype</c> shape
/// only -- the document names no closed list of accepted types, and this framework is
/// explicitly provider- and business-purpose-agnostic (file-storage.md's own File
/// examples span PDF through ZIP); a business module's own content-type allowlist, if
/// any, is that module's own concern layered on top of this framework, not this
/// framework's.
/// </summary>
public sealed partial class MimeType : ValueObject
{
    private const int _maxLength = 255;

    public string Value { get; }

    private MimeType(string value)
    {
        Value = value;
    }

    public static Result<MimeType> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<MimeType>(FileStorageErrors.MimeTypeRequired);
        }

        var trimmed = value.Trim();

        if (trimmed.Length > _maxLength || !MimeTypePattern().IsMatch(trimmed))
        {
            return Result.Failure<MimeType>(FileStorageErrors.MimeTypeInvalid);
        }

        return Result.Success(new MimeType(trimmed));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"^[a-zA-Z0-9][\w.+-]*/[a-zA-Z0-9][\w.+-]*$")]
    private static partial Regex MimeTypePattern();
}
