using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Hris.SharedKernel;

/// <summary>
/// Grounded in docs/02-architecture/04-domain-driven-design/shared-kernel.md's own
/// "What Belongs in the Shared Kernel" list (Value Objects: "EmailAddress...") and
/// value-objects.md's Personal Information section ("Format validation,
/// Normalization, Case handling"). Built here, in SharedKernel, on first genuine need
/// from Identity Framework's <c>Username</c>/login concept -- not spun up
/// speculatively, since shared-kernel.md already names it explicitly rather than
/// leaving it to be inferred.
///
/// Normalizes by lower-casing only; it does not attempt full RFC 5321/5322
/// validation, which is neither this platform's job nor practically achievable by
/// regex -- the only reliable proof an address is real is delivering to it.
/// </summary>
public sealed partial class EmailAddress : ValueObject
{
    private const int _maxLength = 320;

    public string Value { get; }

    private EmailAddress(string value)
    {
        Value = value;
    }

    [SuppressMessage(
        "Globalization",
        "CA1308:Normalize strings to uppercase",
        Justification = "Email addresses are conventionally normalized and displayed lowercase "
            + "everywhere (RFC 5321 case-insensitivity in practice, every mail client and log "
            + "line) -- uppercasing to satisfy this rule's general dictionary-key-normalization "
            + "rationale would produce a value no one recognizes as an email address.")]
    public static Result<EmailAddress> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<EmailAddress>(SharedKernelErrors.EmailAddressRequired);
        }

        var normalized = value.Trim().ToLowerInvariant();

        if (normalized.Length > _maxLength || !FormatPattern().IsMatch(normalized))
        {
            return Result.Failure<EmailAddress>(SharedKernelErrors.EmailAddressInvalidFormat);
        }

        return Result.Success(new EmailAddress(normalized));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex FormatPattern();
}
