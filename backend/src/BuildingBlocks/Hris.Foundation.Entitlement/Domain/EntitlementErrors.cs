using Hris.SharedKernel;

namespace Hris.Foundation.Entitlement.Domain;

/// <summary>
/// This bounded context's own reusable error catalog, per error-pattern.md's "Error
/// Catalog" section. Both errors below use <see cref="ErrorCategory.Entitlement"/>,
/// never <see cref="ErrorCategory.Authorization"/> -- error-pattern.md's own new
/// "Entitlement Errors" subsection states why the two must never share a category
/// (CTR-ENT-007).
/// </summary>
public static class EntitlementErrors
{
    public static readonly Error PackNotActive = new(
        "Entitlement.PackNotActive",
        "The Process Pack owning this capability is not active for this tenant.",
        ErrorCategory.Entitlement);

    public static readonly Error MaturityLevelInsufficient = new(
        "Entitlement.MaturityLevelInsufficient",
        "This tenant's maturity level for the owning Process Pack is below what this capability requires.",
        ErrorCategory.Entitlement);
}
