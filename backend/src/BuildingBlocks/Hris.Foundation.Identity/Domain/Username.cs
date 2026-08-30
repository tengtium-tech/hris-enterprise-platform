using System.Diagnostics.CodeAnalysis;
using Hris.SharedKernel;

namespace Hris.Foundation.Identity.Domain;

/// <summary>
/// A <see cref="UserAccount"/>'s login handle, per identity-framework.md's User
/// Account section. Normalized to lowercase for the same case-insensitive-login
/// reason <see cref="EmailAddress"/> normalizes -- two accounts differing only by
/// case would otherwise be a tenant-isolation-adjacent source of login confusion.
/// </summary>
public sealed class Username : ValueObject
{
    private const int _minLength = 3;
    private const int _maxLength = 100;

    public string Value { get; }

    private Username(string value)
    {
        Value = value;
    }

    [SuppressMessage(
        "Globalization",
        "CA1308:Normalize strings to uppercase",
        Justification = "Login handles are conventionally compared and displayed lowercase; "
            + "see EmailAddress.Create's identical, more fully justified suppression.")]
    public static Result<Username> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<Username>(IdentityErrors.UsernameRequired);
        }

        var normalized = value.Trim().ToLowerInvariant();

        if (normalized.Length is < _minLength or > _maxLength)
        {
            return Result.Failure<Username>(IdentityErrors.UsernameInvalidLength);
        }

        return Result.Success(new Username(normalized));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
