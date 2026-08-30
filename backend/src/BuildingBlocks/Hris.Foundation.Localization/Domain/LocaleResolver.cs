namespace Hris.Foundation.Localization.Domain;

/// <summary>
/// Implements localization-framework.md's own Implementation Guidance: "Resolve
/// locale from tenant and user preference, with a documented fallback." The
/// documented fallback is stated here, concretely, since the source document names
/// the requirement without stating the precedence order itself: user preference,
/// then tenant default, then the mandatory platform default.
///
/// A stateless static method rather than an instantiable Domain Service
/// (contrast <see cref="RuleConditionEvaluator"/>, the same shape in Rules Engine):
/// there is no repository lookup to perform here, only a pure choice among values the
/// caller already resolved and supplied -- this method never reads a user's stored
/// preference or a tenant's stored default itself, keeping the "user or tenant data"
/// resolution entirely the caller's own responsibility (`CTR-ISO-001`).
/// </summary>
public static class LocaleResolver
{
    public static Locale Resolve(Locale? userPreference, Locale? tenantDefault, Locale platformDefault) =>
        userPreference ?? tenantDefault ?? platformDefault;
}
