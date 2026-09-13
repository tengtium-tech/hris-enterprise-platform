using Hris.SharedKernel;

namespace Hris.Modules.Attendance.Domain;

/// <summary>
/// Every Domain Event this module's six Aggregate Roots raise. Source:
/// docs/04-modules/attendance/domain/domain-events.md.
///
/// Two conventions from that document shape every record below, and both are easy to
/// get wrong in a way that only shows up later.
///
/// First, each event carries the actor where a person decided the change, and omits
/// it where the change is system-initiated (AT-071) — a scheduled recalculation or an
/// automatic device-offline transition was not decided by a person, and a placeholder
/// actor would destroy the audit record's most useful property.
///
/// Second, adjustment approval and its application to the record are two events
/// (<see cref="AttendanceAdjustmentApproved"/> then <see cref="AttendanceAdjustmentApplied"/>)
/// raised in two separate transactions against two separate roots, linked only by the
/// adjustment identifier, never merged into one write (AT-024).
/// </summary>
public sealed record AttendanceRecordCreated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    AttendanceRecordId AttendanceRecordId,
    Guid TenantId,
    Guid EmployeeId,
    DateOnly WorkDate,
    Guid? ActorId) : IDomainEvent;

public sealed record TimeEventCaptured(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    AttendanceRecordId AttendanceRecordId,
    Guid TenantId,
    TimeEventId TimeEventId,
    TimeEventType EventType,
    AttendanceSource Source,
    DateTimeOffset TimestampUtc) : IDomainEvent;

/// <summary>
/// Carries the recalculated result so downstream projections can consume it without
/// re-running the pipeline; the record itself remains the source of truth the
/// pipeline can always reproduce (AT-003).
/// </summary>
public sealed record AttendanceRecordCalculated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    AttendanceRecordId AttendanceRecordId,
    Guid TenantId,
    double WorkingHours,
    double PayableHours,
    double OvertimeHours,
    double LateMinutes,
    double UndertimeMinutes,
    double HolidayHours,
    double NightDifferentialHours,
    string Trigger,
    Guid? ActorId) : IDomainEvent;

public sealed record AttendanceRecordSubmittedForApproval(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    AttendanceRecordId AttendanceRecordId,
    Guid TenantId,
    Guid ActorId) : IDomainEvent;

public sealed record AttendanceRecordApproved(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    AttendanceRecordId AttendanceRecordId,
    Guid TenantId,
    Guid ApproverId) : IDomainEvent;

public sealed record AttendanceRecordRejected(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    AttendanceRecordId AttendanceRecordId,
    Guid TenantId,
    Guid ApproverId,
    string Reason) : IDomainEvent;

public sealed record AttendanceRecordFinalized(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    AttendanceRecordId AttendanceRecordId,
    Guid TenantId,
    Guid ActorId) : IDomainEvent;

public sealed record AttendanceRecordReopened(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    AttendanceRecordId AttendanceRecordId,
    Guid TenantId,
    Guid ActorId,
    string AuthorizationReference,
    string Reason) : IDomainEvent;

public sealed record AttendanceAdjustmentSubmitted(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    AttendanceAdjustmentId AttendanceAdjustmentId,
    Guid TenantId,
    AttendanceRecordId AttendanceRecordId,
    Guid ActorId) : IDomainEvent;

public sealed record AttendanceAdjustmentReviewed(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    AttendanceAdjustmentId AttendanceAdjustmentId,
    Guid TenantId,
    Guid ReviewerId,
    string Notes) : IDomainEvent;

/// <summary>
/// Raised by <c>ApproveAttendanceAdjustmentCommand</c>; consumed by a separate
/// transaction that issues <c>ApplyAttendanceAdjustmentCommand</c> against the record
/// (AT-024). It carries the approved requested value so the record can apply it
/// without re-reading the adjustment's live state.
/// </summary>
public sealed record AttendanceAdjustmentApproved(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    AttendanceAdjustmentId AttendanceAdjustmentId,
    Guid TenantId,
    AttendanceRecordId AttendanceRecordId,
    Guid ApproverId,
    string RequestedValue,
    string Field) : IDomainEvent;

public sealed record AttendanceAdjustmentRejected(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    AttendanceAdjustmentId AttendanceAdjustmentId,
    Guid TenantId,
    AttendanceRecordId AttendanceRecordId,
    Guid ApproverId,
    string Reason) : IDomainEvent;

public sealed record AttendanceAdjustmentCancelled(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    AttendanceAdjustmentId AttendanceAdjustmentId,
    Guid TenantId,
    AttendanceRecordId AttendanceRecordId,
    Guid ActorId,
    string Reason) : IDomainEvent;

public sealed record AttendanceAdjustmentApplied(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    AttendanceAdjustmentId AttendanceAdjustmentId,
    Guid TenantId,
    AttendanceRecordId AttendanceRecordId) : IDomainEvent;

public sealed record OvertimeRequestSubmitted(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    OvertimeRequestId OvertimeRequestId,
    Guid TenantId,
    Guid EmployeeId,
    DateOnly WorkDate,
    Guid ActorId) : IDomainEvent;

public sealed record OvertimeRequestApproved(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    OvertimeRequestId OvertimeRequestId,
    Guid TenantId,
    Guid ApproverId) : IDomainEvent;

public sealed record OvertimeRequestRejected(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    OvertimeRequestId OvertimeRequestId,
    Guid TenantId,
    Guid ApproverId,
    string Reason) : IDomainEvent;

public sealed record OvertimeRequestCancelled(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    OvertimeRequestId OvertimeRequestId,
    Guid TenantId,
    Guid ActorId,
    string Reason) : IDomainEvent;

public sealed record AttendancePolicyDefined(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    AttendancePolicyId AttendancePolicyId,
    Guid TenantId,
    Guid ActorId) : IDomainEvent;

public sealed record AttendancePolicyPublished(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    AttendancePolicyId AttendancePolicyId,
    Guid TenantId,
    DateOnly EffectiveFrom,
    Guid ActorId) : IDomainEvent;

public sealed record AttendancePolicyRevised(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    AttendancePolicyId PreviousPolicyId,
    Guid TenantId,
    AttendancePolicyId NewPolicyId,
    DateOnly NewEffectiveFrom,
    Guid ActorId) : IDomainEvent;

public sealed record AttendancePolicyAssigned(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    AttendancePolicyId AttendancePolicyId,
    Guid TenantId,
    PolicyAssignmentId PolicyAssignmentId,
    string ScopeTargetId,
    DateOnly EffectiveFrom,
    Guid ActorId) : IDomainEvent;

public sealed record AttendancePolicyUnassigned(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    AttendancePolicyId AttendancePolicyId,
    Guid TenantId,
    PolicyAssignmentId PolicyAssignmentId,
    DateOnly EffectiveTo,
    Guid ActorId) : IDomainEvent;

public sealed record AttendancePolicyRetired(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    AttendancePolicyId AttendancePolicyId,
    Guid TenantId,
    Guid ActorId,
    string Reason) : IDomainEvent;

public sealed record AttendanceDeviceRegistered(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    AttendanceDeviceId AttendanceDeviceId,
    Guid TenantId,
    Guid ActorId) : IDomainEvent;

public sealed record AttendanceDeviceConfigured(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    AttendanceDeviceId AttendanceDeviceId,
    Guid TenantId,
    Guid ActorId) : IDomainEvent;

public sealed record AttendanceDeviceStatusChanged(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    AttendanceDeviceId AttendanceDeviceId,
    Guid TenantId,
    DeviceStatus NewStatus,
    Guid? ActorId) : IDomainEvent;

public sealed record AttendanceDeviceRetired(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    AttendanceDeviceId AttendanceDeviceId,
    Guid TenantId,
    Guid ActorId,
    string Reason) : IDomainEvent;

public sealed record BiometricEnrollmentCreated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    BiometricEnrollmentId BiometricEnrollmentId,
    Guid TenantId,
    Guid EmployeeId,
    BiometricMethod Method,
    Guid ActorId) : IDomainEvent;

public sealed record BiometricEnrollmentActivated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    BiometricEnrollmentId BiometricEnrollmentId,
    Guid TenantId,
    Guid ActorId) : IDomainEvent;

public sealed record BiometricEnrollmentRevoked(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    BiometricEnrollmentId BiometricEnrollmentId,
    Guid TenantId,
    Guid? ActorId,
    string Reason) : IDomainEvent;
