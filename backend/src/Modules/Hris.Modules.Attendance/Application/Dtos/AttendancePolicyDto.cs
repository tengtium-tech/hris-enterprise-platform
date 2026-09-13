namespace Hris.Modules.Attendance.Application.Dtos;

/// <summary>
/// Read shape for <c>AttendancePolicy</c>, flattening its effective-dated configuration
/// sections so a caller sees the full rulebook without re-walking the aggregate
/// (dto-design.md).
/// </summary>
public sealed record AttendancePolicyDto(
    Guid Id,
    Guid TenantId,
    Guid LineageId,
    string Name,
    int Version,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string Status,
    Guid CreatedBy,
    DateTimeOffset CreatedOn,
    WorkingHoursConfigurationDto WorkingHours,
    GracePeriodDto GracePeriod,
    string RoundingRule,
    BreakPolicyConfigurationDto BreakPolicy,
    OvertimePolicyConfigurationDto OvertimePolicy,
    HolidayRestDayConfigurationDto HolidayRestDay,
    IReadOnlyList<PolicyAssignmentDto> Assignments);

/// <summary>Working-hours ruleset, copied from <c>WorkingHoursConfiguration</c>.</summary>
public sealed record WorkingHoursConfigurationDto(
    double StandardDailyHours,
    double MaximumDailyHours,
    double MinimumDailyHours,
    double StandardWeeklyHours,
    TimeOnly? CoreHoursStart,
    TimeOnly? CoreHoursEnd);

/// <summary>Grace period before late classification, in minutes.</summary>
public sealed record GracePeriodDto(int Minutes);

/// <summary>Break ruleset, copied from <c>BreakPolicyConfiguration</c>.</summary>
public sealed record BreakPolicyConfigurationDto(
    bool RequiresBreak,
    int MinimumDurationMinutes,
    int MaximumDurationMinutes,
    bool BreaksPaid);

/// <summary>Overtime ruleset, copied from <c>OvertimePolicyConfiguration</c>.</summary>
public sealed record OvertimePolicyConfigurationDto(
    bool Eligible,
    bool RequiresPriorAuthorization,
    double DailyThresholdHours,
    double WeeklyThresholdHours);

/// <summary>Holiday and rest-day premium ruleset, copied from <c>HolidayRestDayConfiguration</c>.</summary>
public sealed record HolidayRestDayConfigurationDto(bool HolidayPremiumApplies, bool RestDayPremiumApplies);

/// <summary>One scope binding for the policy (child of the policy, never returned alone).</summary>
public sealed record PolicyAssignmentDto(
    Guid Id,
    string ScopeLevel,
    string ScopeTargetId,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    Guid AssignedBy,
    DateTimeOffset AssignedOn);
