namespace Hris.Foundation.Entitlement.Domain;

/// <summary>
/// The outcome of <see cref="EntitlementEvaluator.Evaluate"/>. Mirrors
/// <c>Hris.Foundation.Authorization.Domain.AuthorizationDecision</c>'s own shape --
/// exactly two outcomes, no third ambiguous state -- but carries no
/// <see cref="Hris.SharedKernel.IDomainEvent"/> of its own: entitlement-framework.md's
/// own Domain Events section states this framework raises none, since
/// <see cref="EntitlementEvaluator"/> is a stateless Domain Service with no persisted
/// Aggregate state to raise an event from, and nothing in this Sprint's own scope
/// names a concrete "record every entitlement decision" requirement the way ADR-0002
/// names one for sensitive-resource authorization decisions.
/// </summary>
public sealed class EntitlementDecision
{
    public bool IsEntitled { get; }

    public EntitlementDenialReason? DenialReason { get; }

    private EntitlementDecision(bool isEntitled, EntitlementDenialReason? denialReason)
    {
        IsEntitled = isEntitled;
        DenialReason = denialReason;
    }

    public static EntitlementDecision Entitled() => new(true, null);

    public static EntitlementDecision Denied(EntitlementDenialReason reason) => new(false, reason);
}
