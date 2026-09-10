namespace Hris.Modules.Timekeeping.Application.Dtos;

/// <summary>
/// Read shape for <c>WorkSchedule</c>. Enumerations project as strings, per
/// dto-design.md's convention across every prior module, so a consumer is never
/// coupled to an ordinal — which matters more here than usual, since
/// <c>OrganizationalAssignmentLevel</c>'s ordinal order is load-bearing internally as
/// the precedence key and must not become a public contract.
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

/// <summary>
/// Shared by <see cref="WorkScheduleDto"/> and <see cref="WorkShiftDto"/> — the
/// domain models a break at both grains with one type, and the read shape follows.
/// </summary>
public sealed record BreakRuleDto(
    string Kind, TimeSpan Duration, TimeOnly? WindowStart, TimeOnly? WindowEnd, bool Paid, bool Mandatory);
