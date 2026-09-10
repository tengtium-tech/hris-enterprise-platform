using Hris.SharedKernel;

namespace Hris.Modules.Timekeeping.Domain;

/// <summary>
/// Every Domain Event this module's four Aggregate Roots raise. Source:
/// docs/04-modules/timekeeping/domain/domain-events.md.
///
/// Two conventions from that document shape every record below, and both are easy to
/// get wrong in a way that only shows up later.
///
/// First, each event carries <c>EffectiveFrom</c> — the date the change applies from
/// — separately from <c>OccurredOnUtc</c>, the moment it was raised. Collapsing the
/// two would make a revision published today but effective next month
/// indistinguishable from one effective immediately, which is precisely the
/// distinction TK-002 exists to preserve.
///
/// Second, system-initiated events carry no actor at all rather than a placeholder
/// (TK-051). An automatic expiry and a rotation-generated assignment were not
/// decided by a person, and a placeholder actor would destroy the audit record's
/// most useful property: telling apart what someone decided from what the system
/// executed on schedule.
/// </summary>
public sealed record WorkSchedulePublished(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    WorkScheduleId WorkScheduleId,
    Guid TenantId,
    int Version,
    DateOnly EffectiveFrom,
    Guid PublishedBy) : IDomainEvent;

public sealed record WorkScheduleSuperseded(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    WorkScheduleId WorkScheduleId,
    Guid TenantId,
    int SupersededVersion,
    int NewVersion,
    DateOnly EffectiveFrom) : IDomainEvent;

public sealed record WorkScheduleAssignedToOrganizationalUnit(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    WorkScheduleId WorkScheduleId,
    Guid TenantId,
    ScheduleAssignmentId ScheduleAssignmentId,
    OrganizationalAssignmentLevel TargetLevel,
    string TargetId,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    Guid AssignedBy) : IDomainEvent;

public sealed record WorkScheduleUnassignedFromOrganizationalUnit(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    WorkScheduleId WorkScheduleId,
    Guid TenantId,
    ScheduleAssignmentId ScheduleAssignmentId,
    OrganizationalAssignmentLevel TargetLevel,
    string TargetId,
    DateOnly EffectiveTo,
    Guid UnassignedBy) : IDomainEvent;

/// <summary>
/// Withdraws a schedule from new assignment. Assignments already referencing it are
/// unaffected until they themselves expire or are reassigned.
/// </summary>
public sealed record WorkScheduleRetired(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    WorkScheduleId WorkScheduleId,
    Guid TenantId,
    DateOnly EffectiveFrom,
    Guid RetiredBy) : IDomainEvent;

public sealed record WorkShiftPublished(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    WorkShiftId WorkShiftId,
    Guid TenantId,
    int Version,
    DateOnly EffectiveFrom,
    Guid PublishedBy) : IDomainEvent;

public sealed record WorkShiftSuperseded(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    WorkShiftId WorkShiftId,
    Guid TenantId,
    int SupersededVersion,
    int NewVersion,
    DateOnly EffectiveFrom) : IDomainEvent;

/// <summary>
/// Raised specifically when overtime eligibility changes between versions. A full
/// <see cref="WorkShiftSuperseded"/> also captures it, but this single flag governs
/// downstream overtime calculation in <c>attendance</c>, and a consumer interested
/// only in that dimension should not have to diff two complete shift definitions to
/// notice it changed.
/// </summary>
public sealed record WorkShiftOvertimeEligibilityChanged(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    WorkShiftId WorkShiftId,
    Guid TenantId,
    bool PreviousValue,
    bool NewValue,
    DateOnly EffectiveFrom,
    Guid ChangedBy) : IDomainEvent;

public sealed record WorkShiftRetired(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    WorkShiftId WorkShiftId,
    Guid TenantId,
    DateOnly EffectiveFrom,
    Guid RetiredBy) : IDomainEvent;

/// <summary>
/// <see cref="AssignedBy"/> is null where <see cref="RotationCycleReference"/> is
/// present and the assignment was system-generated (TK-051).
/// </summary>
public sealed record ShiftAssignmentCreated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    ShiftAssignmentId ShiftAssignmentId,
    Guid TenantId,
    AssignmentTargetType TargetType,
    string TargetId,
    OrganizationalAssignmentLevel TargetLevel,
    WorkShiftId WorkShiftId,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    RotationCycleId? RotationCycleReference,
    Guid? AssignedBy) : IDomainEvent;

/// <summary>
/// Raised once for the pair, carrying both sides, rather than as two independent
/// events. A consumer needs to see a swap as one occurrence to reconstruct what
/// happened; two separate reassignment events do not say that the two are related.
/// </summary>
public sealed record ShiftAssignmentSwapped(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    Guid TenantId,
    ShiftAssignmentId PrimaryAssignmentId,
    string PrimaryEmployeeId,
    ShiftAssignmentId SecondaryAssignmentId,
    string SecondaryEmployeeId,
    DateOnly EffectiveFrom,
    bool BothPartiesConsented,
    Guid? OverrideAuthorityReference,
    Guid InitiatedBy) : IDomainEvent;

/// <summary>Raised automatically at end date. No actor, per TK-051.</summary>
public sealed record ShiftAssignmentExpired(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    ShiftAssignmentId ShiftAssignmentId,
    Guid TenantId,
    DateOnly EffectiveTo) : IDomainEvent;

public sealed record ShiftAssignmentCancelled(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    ShiftAssignmentId ShiftAssignmentId,
    Guid TenantId,
    string Reason,
    Guid CancelledBy,
    DateOnly EffectiveDate) : IDomainEvent;

public sealed record HolidayCalendarPublished(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    HolidayCalendarId HolidayCalendarId,
    Guid? TenantId,
    int Version,
    DateOnly EffectiveFrom,
    Guid PublishedBy) : IDomainEvent;

public sealed record HolidayCalendarSuperseded(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    HolidayCalendarId HolidayCalendarId,
    Guid? TenantId,
    int SupersededVersion,
    int NewVersion,
    DateOnly EffectiveFrom) : IDomainEvent;

public sealed record HolidayAdded(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    HolidayCalendarId HolidayCalendarId,
    Guid? TenantId,
    HolidayId HolidayId,
    DateOnly Date,
    HolidayType Type,
    HolidayWorkRule WorkRule,
    Guid ChangedBy) : IDomainEvent;

public sealed record HolidayRemoved(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    HolidayCalendarId HolidayCalendarId,
    Guid? TenantId,
    HolidayId HolidayId,
    DateOnly Date,
    Guid ChangedBy) : IDomainEvent;

/// <summary>
/// Raised when a parent-calendar link is added, changed, or removed. This changes
/// what the resolved holiday set for the child scope is without any change to the
/// child calendar's own entries, which is why it warrants its own event.
/// </summary>
public sealed record HolidayCalendarLayerChanged(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    HolidayCalendarId HolidayCalendarId,
    Guid? TenantId,
    HolidayCalendarId? PreviousParentCalendarId,
    HolidayCalendarId? NewParentCalendarId,
    DateOnly EffectiveFrom,
    Guid ChangedBy) : IDomainEvent;
