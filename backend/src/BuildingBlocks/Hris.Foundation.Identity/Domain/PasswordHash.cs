using Hris.SharedKernel;

namespace Hris.Foundation.Identity.Domain;

/// <summary>
/// An opaque, already-hashed credential value, per identity-framework.md's Security
/// Considerations: "Store credentials using an industry-standard adaptive hashing
/// algorithm (`NFR-SE-006`)... Never log, cache in plain text, or return credentials
/// from any endpoint or error message."
///
/// The Domain layer never hashes a password itself -- computing a bcrypt/Argon2/PBKDF2
/// digest is a cryptography library concern, i.e. Infrastructure (`CTR-ARC-001`).
/// <see cref="Create"/> accepts the *already-hashed* value an
/// <c>IPasswordHasher</c>-shaped Infrastructure service produced; <see cref="ToString"/>
/// is overridden so this value can never appear in a log line or exception message by
/// accident, structurally enforcing the "never log" rule above rather than relying on
/// every call site remembering not to (this project's own engineering principle:
/// "Prefer structure over discipline").
/// </summary>
public sealed class PasswordHash : ValueObject
{
    public string Value { get; }

    private PasswordHash(string value)
    {
        Value = value;
    }

    public static Result<PasswordHash> Create(string? alreadyHashedValue)
    {
        return string.IsNullOrWhiteSpace(alreadyHashedValue)
            ? Result.Failure<PasswordHash>(IdentityErrors.PasswordHashRequired)
            : Result.Success(new PasswordHash(alreadyHashedValue));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => "***REDACTED***";
}
