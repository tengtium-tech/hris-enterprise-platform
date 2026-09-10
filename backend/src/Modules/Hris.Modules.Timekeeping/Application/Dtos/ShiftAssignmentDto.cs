namespace Hris.Modules.Timekeeping.Application.Dtos;

/// <summary>Read shape for <c>ShiftAssignment</c>.</summary>
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
