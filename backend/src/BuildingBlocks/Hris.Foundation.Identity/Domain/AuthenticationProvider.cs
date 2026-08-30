using Hris.SharedKernel;

namespace Hris.Foundation.Identity.Domain;

/// <summary>
/// Which identity provider authenticated a <see cref="UserAccount"/>, per
/// identity-framework.md's Identity Providers section. A validated string key rather
/// than a closed enum: that section's own provider list ("Microsoft Entra ID, Active
/// Directory, LDAP, Google Identity, Okta, Auth0, Keycloak, Azure AD B2C") is
/// explicitly illustrative ("Supported providers *may include*"), and "The framework
/// should support multiple identity providers simultaneously" -- an enum would force
/// a code change to add one, exactly what an open, tenant-configurable set must not
/// require. Actual OIDC/SAML/WS-Federation protocol handling is Infrastructure, never
/// Domain (`CTR-ARC-001`); this type only records which provider vouched for the
/// account.
/// </summary>
public sealed class AuthenticationProvider : ValueObject
{
    public const string LocalKey = "Local";

    public string Key { get; }

    private AuthenticationProvider(string key)
    {
        Key = key;
    }

    public static Result<AuthenticationProvider> Create(string? key)
    {
        return string.IsNullOrWhiteSpace(key)
            ? Result.Failure<AuthenticationProvider>(IdentityErrors.AuthenticationProviderRequired)
            : Result.Success(new AuthenticationProvider(key.Trim()));
    }

    public static AuthenticationProvider Local() => new(LocalKey);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Key;
    }

    public override string ToString() => Key;
}
