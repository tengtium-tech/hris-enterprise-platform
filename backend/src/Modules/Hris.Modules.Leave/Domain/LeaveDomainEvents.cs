using Hris.SharedKernel;

namespace Hris.Modules.Leave.Domain;

/// <summary>
/// Domain Events raised within the Leave module, per domain-events.md. Grouped in one
/// file per this module's own File Organization convention
/// (docs/08-devops/coding-standards.md); added to as each aggregate is built.
/// </summary>
public sealed record LeaveTypeDefined(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    LeaveTypeId LeaveTypeId,
    Guid TenantId,
    string Code,
    Guid ActorId) : IDomainEvent;

public sealed record LeaveTypeDeactivated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    LeaveTypeId LeaveTypeId,
    Guid TenantId,
    Guid ActorId,
    string Reason) : IDomainEvent;

public sealed record LeavePolicyPublished(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    LeavePolicyId LeavePolicyId,
    Guid TenantId,
    DateOnly EffectiveFrom,
    Guid ActorId) : IDomainEvent;

public sealed record LeavePolicyAssigned(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    LeavePolicyId LeavePolicyId,
    Guid TenantId,
    PolicyAssignmentId PolicyAssignmentId,
    string ScopeTargetId,
    Guid ActorId) : IDomainEvent;

/// <summary>Raised on the version <see cref="LeavePolicy.Revise"/> is called against, naming the version that supersedes it.</summary>
public sealed record LeavePolicySuperseded(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    LeavePolicyId LeavePolicyId,
    Guid TenantId,
    LeavePolicyId SupersededByLeavePolicyId,
    Guid ActorId) : IDomainEvent;

/// <summary>Raised whenever any <see cref="LeaveLedgerEntry"/> is written, of any <see cref="LeaveLedgerEntryType"/>.</summary>
public sealed record LeaveLedgerEntryAppended(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    LeaveBalanceId LeaveBalanceId,
    Guid TenantId,
    LeaveLedgerEntryId LeaveLedgerEntryId,
    LeaveLedgerEntryType EntryType,
    decimal Amount) : IDomainEvent;

public sealed record LeaveBalanceAccrued(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    LeaveBalanceId LeaveBalanceId,
    Guid TenantId,
    decimal Amount,
    DateOnly EffectiveDate) : IDomainEvent;

public sealed record LeaveCarriedOver(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    LeaveBalanceId LeaveBalanceId,
    Guid TenantId,
    decimal Amount,
    DateOnly EffectiveDate) : IDomainEvent;

public sealed record LeaveForfeited(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    LeaveBalanceId LeaveBalanceId,
    Guid TenantId,
    decimal Amount,
    DateOnly EffectiveDate) : IDomainEvent;

/// <summary>A verification/repair resummation completed (LV-025) — raised whether or not the total actually changed.</summary>
public sealed record LeaveBalanceRecalculated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    LeaveBalanceId LeaveBalanceId,
    Guid TenantId,
    Guid ActorId,
    bool WasCorrected) : IDomainEvent;

/// <summary>Raised when a <see cref="LeaveRequest"/> is submitted. Also an integration event — see integration-events.md.</summary>
public sealed record LeaveRequested(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    LeaveRequestId LeaveRequestId,
    Guid TenantId,
    Guid EmployeeId,
    LeaveDateRange DateRange,
    Guid SubmittedBy) : IDomainEvent;

/// <summary>
/// The module's most consequential event (README.md): consumed today by the already-built
/// <c>../../attendance</c> module for absence exclusion (AT-061), and by <c>payroll</c> once
/// it exists. Carries <see cref="Domain.PayTreatment"/> directly so no consumer needs to
/// reach back into this module's own tables. Also an integration event — integration-
/// events.md's own documented payload omits <see cref="LeaveTypeId"/> (external consumers
/// don't need it), but the in-process balance-deduction subscriber does, to resolve which
/// <c>LeaveBalance</c> to deduct from; a domain event may carry more than its published
/// integration-event contract requires.
/// </summary>
public sealed record LeaveApproved(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    LeaveRequestId LeaveRequestId,
    Guid TenantId,
    Guid EmployeeId,
    LeaveTypeId LeaveTypeId,
    LeaveDateRange DateRange,
    PayTreatment PayTreatment,
    decimal PaidDays,
    Guid ApproverId) : IDomainEvent;

public sealed record LeaveRejected(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    LeaveRequestId LeaveRequestId,
    Guid TenantId,
    Guid ApproverId,
    string Reason) : IDomainEvent;

/// <summary>
/// Also an integration event (see <see cref="LeaveApproved"/>'s own remarks on why this
/// carries <see cref="LeaveTypeId"/> beyond integration-events.md's documented minimum).
/// <see cref="WasApproved"/> tells the balance-side subscriber whether a compensating
/// entry is needed — only an already-Approved request had a deduction to reverse.
/// </summary>
public sealed record LeaveCancelled(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    LeaveRequestId LeaveRequestId,
    Guid TenantId,
    Guid EmployeeId,
    LeaveTypeId LeaveTypeId,
    LeaveDateRange DateRange,
    bool WasApproved,
    decimal PaidDaysToRestore,
    Guid ActorId) : IDomainEvent;

/// <summary>
/// Raised at approval when the finalized <see cref="Domain.PayTreatment"/> is not fully
/// <see cref="Domain.PayTreatment.Paid"/> (LV-086). Also an integration event — the fact
/// <c>payroll</c> will consume once it exists.
/// </summary>
public sealed record LWOPPeriodRecorded(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    LeaveRequestId LeaveRequestId,
    Guid TenantId,
    Guid EmployeeId,
    LeaveDateRange DateRange) : IDomainEvent;

public sealed record LeaveAdjustmentSubmitted(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    LeaveAdjustmentId LeaveAdjustmentId,
    Guid TenantId,
    LeaveBalanceId LeaveBalanceId,
    Guid SubmittedBy) : IDomainEvent;

public sealed record LeaveAdjustmentApproved(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    LeaveAdjustmentId LeaveAdjustmentId,
    Guid TenantId,
    LeaveBalanceId LeaveBalanceId,
    decimal RequestedAmount,
    Guid ApproverId) : IDomainEvent;

public sealed record LeaveAdjustmentRejected(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    LeaveAdjustmentId LeaveAdjustmentId,
    Guid TenantId,
    Guid ApproverId,
    string Reason) : IDomainEvent;

/// <summary>Raised by <see cref="LeaveBalance.RecordAdjustment"/> — the target confirming incorporation, not the source (LV-052).</summary>
public sealed record LeaveAdjustmentApplied(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    LeaveAdjustmentId LeaveAdjustmentId,
    LeaveBalanceId LeaveBalanceId,
    Guid TenantId) : IDomainEvent;
