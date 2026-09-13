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

    public static LeaveBalanceDto ToDto(LeaveBalance balance)
    {
        ArgumentNullException.ThrowIfNull(balance);

        return new LeaveBalanceDto(
            balance.Id.Value, balance.TenantId, balance.EmployeeId, balance.LeaveTypeId.Value, balance.CurrentBalance,
            balance.LastRecalculatedAt);
    }

    public static LeaveLedgerEntryDto ToDto(LeaveLedgerEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        return new LeaveLedgerEntryDto(
            entry.Id.Value, entry.EntryType.ToString(), entry.Amount, entry.EffectiveDate, entry.SourceReference, entry.Actor,
            entry.RecordedAt);
    }

    public static LeaveRequestDto ToDto(LeaveRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new LeaveRequestDto(
            request.Id.Value,
            request.TenantId,
            request.EmployeeId,
            request.LeaveTypeId.Value,
            request.DateRange.StartDate,
            request.DateRange.EndDate,
            request.DateRange.HalfDayAtStart,
            request.DateRange.HalfDayAtEnd,
            request.DateRange.RequestedDays,
            request.Status.ToString(),
            request.PayTreatment?.ToString(),
            request.PaidDays,
            request.SubmittedBy,
            request.SubmittedOn,
            request.Decision?.ApproverId,
            request.RejectionReason,
            request.CancellationReason);
    }

    public static LeaveAdjustmentDto ToDto(LeaveAdjustment adjustment)
    {
        ArgumentNullException.ThrowIfNull(adjustment);

        return new LeaveAdjustmentDto(
            adjustment.Id.Value,
            adjustment.TenantId,
            adjustment.LeaveBalanceId.Value,
            adjustment.OriginalValueSnapshot,
            adjustment.RequestedAmount,
            adjustment.Reason,
            adjustment.SupportingDocuments,
            adjustment.Status.ToString(),
            adjustment.SubmittedBy,
            adjustment.SubmittedOn,
            adjustment.ReviewerId,
            adjustment.ReviewNotes,
            adjustment.Decision?.ApproverId,
            adjustment.RejectionReason,
            adjustment.AppliedAt);
    }

    public static LeaveEncashmentDto ToDto(LeaveEncashment encashment)
    {
        ArgumentNullException.ThrowIfNull(encashment);

        return new LeaveEncashmentDto(
            encashment.Id.Value,
            encashment.TenantId,
            encashment.EmployeeId,
            encashment.LeaveBalanceId.Value,
            encashment.RequestedAmount,
            encashment.Status.ToString(),
            encashment.SubmittedBy,
            encashment.SubmittedOn,
            encashment.Decision?.ApproverId,
            encashment.RejectionReason,
            encashment.CancellationReason);
    }
}
