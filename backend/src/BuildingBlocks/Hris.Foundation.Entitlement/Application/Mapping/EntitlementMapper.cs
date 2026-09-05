using Hris.Foundation.Entitlement.Application.Dtos;
using Hris.Foundation.Entitlement.Domain;

namespace Hris.Foundation.Entitlement.Application.Mapping;

/// <summary>
/// Maps <see cref="EntitlementDecision"/>/<see cref="ProcessPackCode"/> catalogue
/// entries to their query-side DTOs, by hand rather than through a registered mapping
/// profile -- the identical deviation <c>AuthorizationMapper</c> states and justifies
/// for the same reason: every field here either converts an enum to its DTO-side
/// string or reads a static catalogue lookup.
/// </summary>
internal static class EntitlementMapper
{
    public static EntitlementDecisionDto ToDto(this EntitlementDecision decision) =>
        new(decision.IsEntitled, decision.DenialReason?.ToString());

    public static ProcessPackDto ToDto(this ProcessPackCode pack) => new(
        pack.ToString(),
        ProcessPackCatalog.GetDisplayName(pack),
        ProcessPackCatalog.IsCore(pack),
        ProcessPackCatalog.GetConditionalDependencies(pack).Select(dependency => dependency.ToString()).ToList());

    public static PackEntitlementDto ToPackEntitlementDto(this ProcessPackCode pack, TenantEditionCode edition)
    {
        var isCore = ProcessPackCatalog.IsCore(pack);
        var defaultMaturityLevel = isCore ? null : EditionDefaultPackComposition.TryGetDefaultMaturityLevel(edition, pack);
        var isEntitled = isCore || defaultMaturityLevel is not null;

        return new PackEntitlementDto(
            pack.ToString(),
            ProcessPackCatalog.GetDisplayName(pack),
            isCore,
            isEntitled,
            defaultMaturityLevel?.ToString());
    }
}
