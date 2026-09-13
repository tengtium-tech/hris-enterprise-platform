namespace Hris.Modules.Leave.Application.Dtos;

/// <summary>
/// Read shape for <c>LeavePolicy</c>, flattening its ruleset sections so a caller sees
/// the full rulebook without re-walking the aggregate (dto-design.md).
/// </summary>
public sealed record LeavePolicyDto(
    Guid Id,
    Guid TenantId,
    Guid LeaveTypeId,
    Guid LineageId,
    int Version,
    DateOnly? EffectiveFrom,
    DateOnly? EffectiveTo,
    string Status,
    AccrualRuleDto AccrualRule,
    EligibilityCriteriaDto EligibilityCriteria,
    EntitlementCapDto EntitlementCap,
    CarryoverRuleDto CarryoverRule,
    CommutabilityStatusDto CommutabilityStatus,
    Guid CreatedBy,
    DateTimeOffset CreatedOn,
    IReadOnlyList<PolicyAssignmentDto> Assignments);

/// <summary>Copied from <c>AccrualRule</c>.</summary>
public sealed record AccrualRuleDto(string Method, decimal? Rate, string? Frequency, bool ProratesForMidPeriodChange);

/// <summary>Copied from <c>EligibilityCriteria</c>.</summary>
public sealed record EligibilityCriteriaDto(
    int MinimumTenureMonths, IReadOnlyList<string> EligibleEmploymentStatuses, IReadOnlyList<string> EligibleEmploymentTypes);

/// <summary>Copied from <c>EntitlementCap</c>.</summary>
public sealed record EntitlementCapDto(decimal MaximumAccruable, decimal? MaximumPerRequest);

/// <summary>Copied from <c>CarryoverRule</c>.</summary>
public sealed record CarryoverRuleDto(decimal MaximumCarryover, int? ExpiryGraceDays, string ExcessTreatment);

/// <summary>Copied from <c>CommutabilityStatus</c>.</summary>
public sealed record CommutabilityStatusDto(bool IsCommutable, decimal? MaximumCommutable);

/// <summary>One scope binding for the policy (child of the policy, never returned alone).</summary>
public sealed record PolicyAssignmentDto(
    Guid Id, string ScopeLevel, string ScopeTargetId, DateOnly EffectiveFrom, DateOnly? EffectiveTo, Guid AssignedBy,
    DateTimeOffset AssignedOn);
