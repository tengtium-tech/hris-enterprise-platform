using Hris.Modules.Leave.Application.Dtos;
using Hris.Modules.Leave.Domain;

namespace Hris.Modules.Leave.Application.Mapping;

/// <summary>Maps Leave aggregates to their DTOs (application/mapping.md). Grown as each aggregate is built.</summary>
public static class LeaveMapper
{
    public static LeaveTypeDto ToDto(LeaveType leaveType)
    {
        ArgumentNullException.ThrowIfNull(leaveType);

        return new LeaveTypeDto(
            leaveType.Id.Value,
            leaveType.TenantId,
            leaveType.Code,
            leaveType.Name,
            leaveType.Category.ToString(),
            leaveType.Scope.ToString(),
            leaveType.StatutoryBasis,
            leaveType.StatutoryMinimum,
            leaveType.Status.ToString());
    }

    public static LeavePolicyDto ToDto(LeavePolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        var ruleset = policy.Ruleset;

        return new LeavePolicyDto(
            policy.Id.Value,
            policy.TenantId,
            policy.LeaveTypeId.Value,
            policy.LineageId,
            policy.Version,
            policy.EffectiveFrom,
            policy.EffectiveTo,
            policy.Status.ToString(),
            new AccrualRuleDto(
                ruleset.AccrualRule.Method.ToString(), ruleset.AccrualRule.Rate, ruleset.AccrualRule.Frequency?.ToString(),
                ruleset.AccrualRule.ProratesForMidPeriodChange),
            new EligibilityCriteriaDto(
                ruleset.EligibilityCriteria.MinimumTenureMonths, ruleset.EligibilityCriteria.EligibleEmploymentStatuses,
                ruleset.EligibilityCriteria.EligibleEmploymentTypes),
            new EntitlementCapDto(ruleset.EntitlementCap.MaximumAccruable, ruleset.EntitlementCap.MaximumPerRequest),
            new CarryoverRuleDto(
                ruleset.CarryoverRule.MaximumCarryover, ruleset.CarryoverRule.ExpiryGraceDays,
                ruleset.CarryoverRule.ExcessTreatment.ToString()),
            new CommutabilityStatusDto(ruleset.CommutabilityStatus.IsCommutable, ruleset.CommutabilityStatus.MaximumCommutable),
            policy.CreatedBy,
            policy.CreatedOn,
            policy.PolicyAssignments
                .Select(a => new PolicyAssignmentDto(
                    a.Id.Value, a.ScopeLevel.ToString(), a.ScopeTargetId, a.EffectiveFrom, a.EffectiveTo, a.AssignedBy, a.AssignedOn))
                .ToList());
    }
}
