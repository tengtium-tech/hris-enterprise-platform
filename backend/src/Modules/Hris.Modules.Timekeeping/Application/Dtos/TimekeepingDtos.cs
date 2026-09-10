namespace Hris.Modules.Timekeeping.Application.Dtos;

/// <summary>
/// Read shapes for this module. Enumerations project as strings, per dto-design.md's
/// convention across every prior module, so a consumer is never coupled to an
/// ordinal — which matters more here than usual, since
/// <c>OrganizationalAssignmentLevel</c>'s ordinal order is load-bearing internally
/// as the precedence key and must not become a public contract.
/// </summary>
public sealed record WorkScheduleDto(
    Guid Id,
    Guid TenantId,
    Guid LineageId,
    string Name,
    string? Description,
    IReadOnlyList<string> WorkingDays,
    IReadOnlyList<string> RestDays,
    TimeOnly? StandardHoursStart,
    TimeOnly? StandardHoursEnd,
    IReadOnlyList<BreakRuleDto> BreakPeriods,
    int Version,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string Status,
    IReadOnlyList<ScheduleAssignmentDto> ScheduleAssignments);

public sealed record ScheduleAssignmentDto(
    Guid Id,
    string TargetLevel,
    string TargetId,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    Guid AssignedBy,
    DateTimeOffset AssignedOn);

public sealed record BreakRuleDto(
    string Kind, TimeSpan Duration, TimeOnly? WindowStart, TimeOnly? WindowEnd, bool Paid, bool Mandatory);

public sealed record WorkShiftDto(
    Guid Id,
    Guid TenantId,
    Guid LineageId,
    string Code,
    string Name,
    ShiftTimingDto Timing,
    bool IsOvernight,
    WorkDateAnchorRuleDto? AnchorRule,
    IReadOnlyList<ShiftPeriodDto> SplitPeriods,
    IReadOnlyList<BreakRuleDto> BreakRules,
    bool OvertimeEligible,
    PremiumEligibilityDto PremiumEligibility,
    int Version,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string Status);

public sealed record ShiftTimingDto(
    string Kind,
    TimeOnly? FixedStart,
    TimeOnly? FixedEnd,
    TimeOnly? EarliestStart,
    TimeOnly? LatestStart,
    TimeOnly? CoreStart,
    TimeOnly? CoreEnd,
    TimeSpan? RequiredHours);

public sealed record WorkDateAnchorRuleDto(string AnchorPoint, string Description);

public sealed record ShiftPeriodDto(int Sequence, TimeOnly Start, TimeOnly End);

/// <summary>Flags only — no rate, per the module's ownership boundary with payroll.</summary>
public sealed record PremiumEligibilityDto(
    bool NightDifferentialEligible, bool HazardEligible, bool HolidayPremiumEligible);

public sealed record ShiftAssignmentDto(
    Guid Id,
    Guid TenantId,
    string TargetType,
    string TargetId,
    string TargetLevel,
    Guid WorkShiftId,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string Status,
    Guid? RotationCycleReference,
    Guid? SwapLinkedAssignmentId,
    PendingShiftSwapDto? PendingSwap,
    Guid? AssignedBy,
    DateTimeOffset AssignedOn);

/// <summary>
/// Both employee identities are carried, never just one. An audit record naming only
/// the acting party describes a swap the other party may never have agreed to.
/// </summary>
public sealed record PendingShiftSwapDto(
    Guid CounterpartAssignmentId,
    DateOnly ProposedEffectiveFrom,
    Guid InitiatedBy,
    string PrimaryEmployeeId,
    string CounterpartEmployeeId,
    bool PrimaryConsented,
    bool CounterpartConsented,
    Guid? OverriddenBy,
    string? OverrideReason,
    bool IsReadyToActivate);

public sealed record HolidayCalendarDto(
    Guid Id,
    Guid? TenantId,
    Guid LineageId,
    string Name,
    string Level,
    string ScopeTargetId,
    string CountryCode,
    Guid? ParentCalendarId,
    IReadOnlyList<HolidayDto> Holidays,
    int Version,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string Status);

public sealed record HolidayDto(Guid Id, DateOnly Date, string Name, string Type, string WorkRule);

/// <summary>
/// The answer to "which shift does this employee owe on this date". Carries the
/// unresolved and ambiguous states explicitly rather than returning null for both:
/// a gap in configuration and a conflict in it are different problems with different
/// remedies (TK-030).
/// </summary>
public sealed record ShiftResolutionDto(
    ShiftAssignmentDto? Assignment, Guid? WorkShiftId, bool IsUnresolved, bool IsAmbiguous);

/// <summary>
/// The answer to "is this date a holiday for this scope", including which layer
/// supplied it so the determination is explainable rather than merely asserted.
/// </summary>
public sealed record HolidayResolutionDto(
    HolidayDto? Holiday, Guid? SourceCalendarId, string? SourceLevel, bool IsHoliday);
