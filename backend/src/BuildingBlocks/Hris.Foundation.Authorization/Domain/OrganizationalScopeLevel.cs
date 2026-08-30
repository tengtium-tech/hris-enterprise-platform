namespace Hris.Foundation.Authorization.Domain;

/// <summary>
/// The organizational levels a <see cref="Role"/> can be granted at, per
/// authorization-framework.md's Delegated Administration examples ("Departmental HR
/// administration," "Regional HR administration" = <c>BusinessUnit</c>, "Legal
/// entity HR administration," "Payroll administration for one entity" =
/// <c>LegalEntity</c>).
///
/// A deliberately different, narrower set than <c>ConfigurationScopeLevel</c>
/// (Global, Tenant, Company, LegalEntity, BusinessUnit, Department,
/// IndividualOverride): despite five overlapping names, the two enums answer
/// different questions -- which configuration value applies, versus which
/// organizational unit a role grant reaches -- and this document's own examples
/// never scope a role grant at "Global" (a role delegation is always within some
/// tenant) or "IndividualOverride" (a role is granted to a principal directly, not
/// layered as a per-record override the way a configuration value is). Reusing the
/// Configuration enum here would let a grant nonsensically claim either level; a
/// purpose-fit enum makes that state unrepresentable instead of merely unused.
/// Ordinal order is meaningful for the same "more specific" comparison
/// <c>ConfigurationScopeLevel</c> documents.
/// </summary>
public enum OrganizationalScopeLevel
{
    Tenant = 0,
    Company = 1,
    LegalEntity = 2,
    BusinessUnit = 3,
    Department = 4,
}
