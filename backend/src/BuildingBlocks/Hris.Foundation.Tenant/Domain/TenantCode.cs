using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Hris.SharedKernel;

namespace Hris.Foundation.Tenant.Domain;

/// <summary>
/// A tenant's globally unique code, per tenant-framework.md's Tenant Aggregate/Owns
/// section and client-tenant-onboarding.md (BUS-246): "The tenant code (subdomain)
/// requested is available and not already registered to another tenant." Validated
/// here as a DNS subdomain label (RFC 1035): lowercase letters, digits, and internal
/// hyphens only, 3-63 characters, never starting or ending with a hyphen -- the format
/// the "(subdomain)" parenthetical commits to, since this value resolves tenant
/// context on every request (Tenant Aggregate, Invariants: "Tenant Code is immutable
/// once set... used to resolve tenant context on every request").
///
/// Global uniqueness itself is not this type's concern -- a Value Object validates its
/// own shape only (value-objects.md); <see cref="ITenantRepository.ExistsByTenantCodeAsync"/>
/// is where <c>RegisterTenantCommand</c>'s own handler checks it, the same split
/// <c>CreateCountryConfigurationCommandHandler</c> already establishes for its own
/// per-country uniqueness check.
/// </summary>
public sealed partial class TenantCode : ValueObject
{
    private const int _minLength = 3;
    private const int _maxLength = 63;

    public string Value { get; }

    private TenantCode(string value)
    {
        Value = value;
    }

    [SuppressMessage(
        "Globalization",
        "CA1308:Normalize strings to uppercase",
        Justification = "A tenant code is a DNS subdomain label (this type's own remarks); " +
            "subdomains are conventionally lowercase, and CA1308's usual security concern " +
            "(a lowercase transform being used for a security-sensitive comparison prone to " +
            "culture-specific casing bugs) does not apply here: this value is compared only " +
            "against another already-normalized TenantCode's own Value, both produced by this " +
            "same method, and InvariantCulture eliminates the locale-dependent casing CA1308 " +
            "warns about in the first place.")]
    public static Result<TenantCode> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<TenantCode>(TenantErrors.TenantCodeRequired);
        }

        var normalized = value.Trim().ToLowerInvariant();

        if (normalized.Length is < _minLength or > _maxLength || !SubdomainLabelPattern().IsMatch(normalized))
        {
            return Result.Failure<TenantCode>(TenantErrors.TenantCodeInvalidFormat);
        }

        return Result.Success(new TenantCode(normalized));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    [GeneratedRegex("^[a-z0-9]([a-z0-9-]*[a-z0-9])?$")]
    private static partial Regex SubdomainLabelPattern();
}
