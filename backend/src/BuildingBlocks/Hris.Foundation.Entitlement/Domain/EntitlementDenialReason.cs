namespace Hris.Foundation.Entitlement.Domain;

/// <summary>
/// Distinguishes the two ways <see cref="EntitlementEvaluator.Evaluate"/> can deny a
/// request, per entitlement-framework.md's own evaluation diagram. Carried on
/// <see cref="EntitlementDecision"/> so a caller (and, eventually, an API response)
/// can tell "this pack is not active at all" from "this pack is active but not
/// mature enough" -- both are commercial upgrade prompts, but they point the tenant
/// at a different purchase.
/// </summary>
public enum EntitlementDenialReason
{
    PackNotActive = 0,
    MaturityLevelInsufficient = 1,
}
