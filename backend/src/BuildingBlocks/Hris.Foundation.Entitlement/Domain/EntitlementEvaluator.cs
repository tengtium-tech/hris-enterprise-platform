namespace Hris.Foundation.Entitlement.Domain;

/// <summary>
/// Implements entitlement-framework.md's own Entitlement Evaluation section --
/// Stage 3 of the platform's request pipeline. A stateless Domain Service, the same
/// shape <c>Hris.Foundation.Authorization.Domain.AuthorizationEvaluator</c> already
/// establishes, but synchronous: unlike that evaluator, this one reads only two
/// in-memory tables (<see cref="ProcessPackCatalog"/>, <see cref="EditionDefaultPackComposition"/>)
/// and the caller's own already-resolved <see cref="TenantEditionCode"/> -- no
/// repository, no I/O, so no <see cref="Task"/> wrapper is warranted at this layer;
/// <c>EvaluateEntitlementQueryHandler</c> is the layer that satisfies MediatR's own
/// async contract.
///
/// Default is always deny: a Core pack short-circuits to entitled immediately
/// (CTR-ENT-008); every other path is a denial until the composition table proves
/// otherwise, the same "default behavior should deny access when authorization
/// cannot be determined" discipline authorization-framework.md's own Permission
/// Evaluation section states, applied here to entitlement.
/// </summary>
public static class EntitlementEvaluator
{
    public static EntitlementDecision Evaluate(TenantEditionCode edition, ProcessPackCode pack, MaturityLevel requiredMaturityLevel)
    {
        if (ProcessPackCatalog.IsCore(pack))
        {
            return EntitlementDecision.Entitled();
        }

        var defaultMaturityLevel = EditionDefaultPackComposition.TryGetDefaultMaturityLevel(edition, pack);
        if (defaultMaturityLevel is null)
        {
            return EntitlementDecision.Denied(EntitlementDenialReason.PackNotActive);
        }

        return defaultMaturityLevel.Value >= requiredMaturityLevel
            ? EntitlementDecision.Entitled()
            : EntitlementDecision.Denied(EntitlementDenialReason.MaturityLevelInsufficient);
    }
}
