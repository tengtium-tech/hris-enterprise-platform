namespace Hris.Modules.Timekeeping.Application.Dtos;

/// <summary>Read shape for <c>WorkShift</c>.</summary>
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
